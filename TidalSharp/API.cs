using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using TidalSharp.Data;
using TidalSharp.Exceptions;

namespace TidalSharp;

public class API
{
    internal API(HttpClient client, Session session)
    {
        _httpClient = client;
        _session = session;
    }

    private HttpClient _httpClient;
    private Session _session;
    private TidalUser? _activeUser;

    private readonly ConcurrentQueue<DateTime> _requestTimestamps = [];
    private readonly TimeSpan _rateLimitTimeWindow = TimeSpan.FromSeconds(1);
    internal int _rateLimitMaxRequestsPerSecond = 5;

    public async Task<JObject> GetTrack(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"tracks/{id}", token: token);
    public async Task<TidalLyrics?> GetTrackLyrics(string id, CancellationToken token = default)
    {
        try
        {
            return (await Call(HttpMethod.Get, $"tracks/{id}/lyrics", token: token)).ToObject<TidalLyrics>()!;
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
    }

    public async Task<JObject> GetAlbum(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"albums/{id}", token: token);
    public async Task<JObject> GetAlbumTracks(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"albums/{id}/tracks", token: token);

    public async Task<JObject> GetArtist(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"artists/{id}", token: token);
    public async Task<JObject> GetArtistAlbums(string id, FilterOptions filter = FilterOptions.ALL, CancellationToken token = default) => await Call(HttpMethod.Get, $"artists/{id}/albums",
        urlParameters: new()
        {
            { "filter", filter.ToString() }
        },
        token: token
    );

    public async Task<JObject> GetPlaylist(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"playlists/{id}", token: token);
    public async Task<JObject> GetPlaylistTracks(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"playlists/{id}/tracks", token: token);

    public async Task<JObject> GetVideo(string id, CancellationToken token = default) => await Call(HttpMethod.Get, $"videos/{id}", token: token);

    public async Task<JObject> GetMix(string id, CancellationToken token = default)
    {
        var result = await Call(HttpMethod.Get, "pages/mix",
            urlParameters: new()
            {
                { "mixId", id },
                { "deviceType", "BROWSER" }
            },
            token: token
        );

        var refactoredObj = new JObject()
        {
            { "mix", result["rows"]![0]!["modules"]![0]!["mix"] },
            { "tracks", result["rows"]![1]!["modules"]![0]!["pagedList"] }
        };

        return refactoredObj;
    }

    internal void UpdateUser(TidalUser user) => _activeUser = user;

    internal async Task<JObject> Call(
        HttpMethod method,
        string path,
        Dictionary<string, string>? formParameters = null,
        Dictionary<string, string>? urlParameters = null,
        Dictionary<string, string>? headers = null,
        string? baseUrl = null,
        CancellationToken token = default
    )
    {
        await EnforceRateLimit();

        headers ??= [];
        urlParameters ??= [];
        urlParameters["sessionId"] = _activeUser?.SessionID ?? "";
        urlParameters["countryCode"] = _activeUser?.CountryCode ?? "";
        urlParameters["limit"] = _session.ItemLimit.ToString();

        if (_activeUser != null)
            headers["Authorization"] = $"{_activeUser.TokenType} {_activeUser.AccessToken}";

        baseUrl ??= Globals.API_V1_LOCATION;

        var apiUrl = CombineUrl(baseUrl, path);

        var stringBuilder = new StringBuilder(apiUrl);
        for (int i = 0; i < urlParameters.Count; i++)
        {
            var start = i == 0 ? "?" : "&";
            var key = WebUtility.UrlEncode(urlParameters.ElementAt(i).Key);
            var value = WebUtility.UrlEncode(urlParameters.ElementAt(i).Value);
            stringBuilder.Append(start + key + "=" + value);
        }

        apiUrl = stringBuilder.ToString();

        var content = formParameters == null ? null : new FormUrlEncodedContent(formParameters);

        var request = new HttpRequestMessage()
        {
            Method = method,
            RequestUri = new Uri(apiUrl),
            Content = content
        };

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value);

        var response = await _httpClient.SendAsync(request, token);

        string resp = await response.Content.ReadAsStringAsync(token);
        JObject json = JObject.Parse(resp);

        if (!response.IsSuccessStatusCode && !string.IsNullOrEmpty(_activeUser?.RefreshToken))
        {
            string? userMessage = json.GetValue("userMessage")?.ToString();
            if (userMessage != null && userMessage.Contains("The token has expired."))
            {
                bool refreshed = await _session.AttemptTokenRefresh(_activeUser, token);
                if (refreshed)
                    return await Call(method, path, formParameters, urlParameters, headers, baseUrl, token);
            }
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            JToken? errors = json["errors"];
            if (errors != null && errors.Any())
                throw new ResourceNotFoundException(errors[0]!["detail"]!.ToString());

            JToken? userMessage = json["userMessage"];
            if (userMessage != null)
                throw new ResourceNotFoundException(userMessage.ToString());

            throw new ResourceNotFoundException(json.ToString());
        }

        if (!response.IsSuccessStatusCode)
        {
            JToken? errors = json["errors"];
            if (errors != null && errors.Any())
                throw new APIException(errors[0]!["detail"]!.ToString());

            JToken? userMessage = json["userMessage"];
            if (userMessage != null)
                throw new APIException(userMessage.ToString());

            throw new APIException(json.ToString());
        }

        return json;
    }

    private async Task EnforceRateLimit()
    {
        if (_rateLimitMaxRequestsPerSecond <= 0)
            return;

        if (!_requestTimestamps.TryPeek(out var timePeek))
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
            return;
        }

        // remove old time stamps
        while (_requestTimestamps.Any() && timePeek < DateTime.UtcNow - _rateLimitTimeWindow)
            _requestTimestamps.TryDequeue(out _);

        // determine if we should be waiting or not
        if (_requestTimestamps.Count >= _rateLimitMaxRequestsPerSecond)
        {
            if (!_requestTimestamps.TryPeek(out timePeek))
                return;

            var nextAvailableTime = timePeek.AddSeconds(1);
            var delayTime = nextAvailableTime - DateTime.UtcNow;
            if (delayTime > TimeSpan.Zero)
                await Task.Delay(delayTime);
        }

        _requestTimestamps.Enqueue(DateTime.UtcNow);
    }

    private static string CombineUrl(params string[] urls)
    {
        var builder = new StringBuilder();
        foreach (var url in urls)
        {
            builder.Append(url.TrimStart('/').TrimEnd('/'));
            builder.Append('/');
        }

        string finalUrl = builder.ToString();

        if (!urls.Last().EndsWith('/'))
            finalUrl = finalUrl.TrimEnd('/');

        return finalUrl;
    }
}
