using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using TidalSharp.Data;

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

    internal void UpdateUser(TidalUser user) => _activeUser = user;

    internal async Task<JObject> Call(
        HttpMethod method,
        string path,
        Dictionary<string, string>? formParameters = null,
        Dictionary<string, string>? urlParameters = null,
        Dictionary<string, string>? headers = null,
        string? baseUrl = null
    )
    {
        headers ??= [];
        urlParameters ??= [];
        urlParameters["sessionId"] = _activeUser?.SessionID ?? "";
        urlParameters["countryCode"] = _activeUser?.CountryCode ?? "";
        urlParameters["limit"] = _session.ItemLimit.ToString();

        if (_activeUser != null && !headers.ContainsKey("Authorization"))
            headers.Add("Authorization", $"{_activeUser.TokenType} {_activeUser.AccessToken}");

        baseUrl ??= Globals.API_V1_LOCATION;

        // TODO: could probably be implemented without adding another library
        var apiUrl = Flurl.Url.Combine(baseUrl, path);

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

        var response = await _httpClient.SendAsync(request);

        string resp = await response.Content.ReadAsStringAsync();
        JObject json = JObject.Parse(resp);

        if (!response.IsSuccessStatusCode && !string.IsNullOrEmpty(_activeUser?.RefreshToken))
        {
            string? userMessage = json.GetValue("userMessage")?.ToString();
            if (userMessage != null && userMessage.StartsWith("The token has expired."))
            {
                bool refreshed = await _session.AttemptTokenRefresh(_activeUser);
                if (refreshed)
                    return await Call(method, path, formParameters, urlParameters, headers, baseUrl);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            // TODO: custom exceptions
            JToken? errors = json["errors"];
            if (errors != null && errors.Any())
                throw new Exception(errors[0]!["detail"]!.ToString());

            JToken? userMessage = json["userMessage"];
            if (userMessage != null)
                throw new Exception(userMessage.ToString());

            throw new Exception(json.ToString());
        }

        return json;
    }
}