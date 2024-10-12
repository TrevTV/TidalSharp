using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using TidalSharp.Data;

namespace TidalSharp;

public class Session
{
    internal Session(HttpClient client, AudioQuality audioQuality, VideoQuality videoQuality, int itemLimit = 1000, bool alac = true)
    {
        AudioQuality = audioQuality;
        VideoQuality = videoQuality;

        _httpClient = client;
        _alac = alac;

        _itemLimit = itemLimit > 10000 ? 10000 : itemLimit;

        _clientUniqueKey = $"{BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0):x}";
        _codeVerifier = ToBase64UrlEncoded(RandomNumberGenerator.GetBytes(32));

        using var sha256 = SHA256.Create();
        _codeChallenge = ToBase64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(_codeVerifier)));
    }

    public AudioQuality AudioQuality { get; init; }
    public VideoQuality VideoQuality { get; init; }

    private HttpClient _httpClient;
    private TidalUser? _activeUser;

    private int _itemLimit;
    private bool _alac;

    private string _clientUniqueKey;
    private string _codeVerifier;
    private string _codeChallenge;

    public void UpdateUser(TidalUser user) => _activeUser = user;

    public string GetPkceLoginUrl()
    {
        var parameters = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "redirect_uri", Globals.PKCE_URI_REDIRECT },
            { "client_id", Globals.CLIENT_ID_PKCE },
            { "lang", "EN" },
            { "appMode", "android" },
            { "client_unique_key", _clientUniqueKey },
            { "code_challenge", _codeChallenge },
            { "code_challenge_method", "S256" },
            { "restrict_signup", "true" }
        };

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in parameters)
        {
            queryString[param.Key] = param.Value;
        }

        return $"{Globals.API_PKCE_AUTH}?{queryString}";
    }

    internal async Task<OAuthTokenData?> GetOAuthDataFromRedirect(string? uri)
    {
        // TODO: custom exceptions for the errors here

        if (string.IsNullOrEmpty(uri) || !uri.StartsWith("https://"))
            throw new Exception("The provided redirect URL looks wrong: " + uri);

        var queryParams = HttpUtility.ParseQueryString(new Uri(uri).Query);
        string? code = queryParams.Get("code");
        if (string.IsNullOrEmpty(code))
            throw new Exception("Authorization code not found in the redirect URL.");

        var data = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", Globals.CLIENT_ID_PKCE },
                { "grant_type", "authorization_code" },
                { "redirect_uri", Globals.PKCE_URI_REDIRECT },
                { "scope", "r_usr+w_usr+w_sub" },
                { "code_verifier", _codeVerifier },
                { "client_unique_key", _clientUniqueKey }
            };

        var content = new FormUrlEncodedContent(data);
        HttpResponseMessage response = await _httpClient.PostAsync(Globals.API_OAUTH2_TOKEN, content);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Login failed: {await response.Content.ReadAsStringAsync()}");

        try
        {
            return JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<OAuthTokenData>();
        }
        catch
        {
            throw new Exception("Invalid response for the authorization code.");
        }
    }

    private static string ToBase64UrlEncoded(byte[] data) => Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").TrimEnd('=');
}