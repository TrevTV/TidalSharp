using Newtonsoft.Json;
using TidalSharp.Data;

namespace TidalSharp;

public class TidalClient
{
    public TidalClient(string? dataDir = null)
    {
        _dataPath = dataDir;
        _userJsonPath = _dataPath == null ? null : Path.Combine(_dataPath, "lastUser.json");

        if (_dataPath != null && !Directory.Exists(_dataPath))
            Directory.CreateDirectory(_dataPath);

        _httpClientHandler = new() { CookieContainer = new() };
        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 12; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("X-Tidal-Token", Globals.CLIENT_ID);

        _session = new(_httpClient);
        API = new(_httpClient, _session);
        Downloader = new(_httpClient, API, _session);
    }

    public API API { get; init; }
    public Downloader Downloader { get; init; }
    public TidalUser? ActiveUser { get; set; }

    private Session _session;

    private string? _dataPath;
    private string? _userJsonPath;
    private string? _lastRedirectUri;

    private HttpClient _httpClient;
    private HttpClientHandler _httpClientHandler;

    public async Task<bool> Login(string? redirectUri = null, CancellationToken token = default)
    {
        var shouldCheckFile = _lastRedirectUri == null || _lastRedirectUri == redirectUri; // prevents us from loading the old user when the redirect uri is updated
        if (shouldCheckFile && await CheckForStoredUser(token))
        {
            _lastRedirectUri = redirectUri;
            return true;
        }

        if (string.IsNullOrEmpty(redirectUri))
            return false;

        var data = await _session.GetOAuthDataFromRedirect(redirectUri, token);
        if (data == null) return false;

        var user = new TidalUser(data, _userJsonPath, true);

        ActiveUser = user;
        API.UpdateUser(user);

        await user.GetSession(API, token);
        await user.WriteToFile(token);

        _lastRedirectUri = redirectUri;

        return true;
    }

    public async Task<bool> IsLoggedIn(CancellationToken token = default)
    {
        if (ActiveUser == null || string.IsNullOrEmpty(ActiveUser.SessionID))
            return false;

        try
        {
            var res = await API.Call(HttpMethod.Get, $"users/{ActiveUser.UserId}/subscription", token: token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetRateLimit(int requestsPerSecond)
    {
        API._rateLimitMaxRequestsPerSecond = requestsPerSecond;
    }

    public string GetPkceLoginUrl() => _session.GetPkceLoginUrl();

    public void RegeneratePkceCodes() => _session.RegenerateCodes();

    private async Task<bool> CheckForStoredUser(CancellationToken token = default)
    {
        if (_userJsonPath != null && File.Exists(_userJsonPath))
        {
            try
            {
                var userData = await File.ReadAllTextAsync(_userJsonPath, token);
                var user = JsonConvert.DeserializeObject<TidalUser>(userData);
                if (user == null) return false;

                user.UpdateJsonPath(_userJsonPath);

                ActiveUser = user;
                API.UpdateUser(user);

                await user.GetSession(API, token);

                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
