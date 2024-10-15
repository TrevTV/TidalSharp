using Newtonsoft.Json;
using TidalSharp.Data;

namespace TidalSharp;

public class TidalClient
{
    public TidalClient(AudioQuality audioQuality = AudioQuality.HIGH, VideoQuality videoQuality = VideoQuality.HIGH, string? dataDir = null)
    {
        _dataPath = dataDir;
        _userJsonPath = _dataPath == null ? null : Path.Combine(_dataPath, "lastUser.json");

        if (_dataPath != null && !Directory.Exists(_dataPath))
            Directory.CreateDirectory(_dataPath);

        _httpClientHandler = new() { CookieContainer = new() };
        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 12; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("X-Tidal-Token", Globals.CLIENT_ID);

        _session = new(_httpClient, audioQuality, videoQuality);
        API = new(_httpClient, _session);
        Downloader = new(_httpClient, API, _session);
    }

    public API API { get; init; }
    public Downloader Downloader { get; init; }
    private TidalUser? ActiveUser { get; set; }

    private Session _session;
    private bool _isPkce;

    private string? _dataPath;
    private string? _userJsonPath;

    private HttpClient _httpClient;
    private HttpClientHandler _httpClientHandler;

    public async Task<bool> Login(string? redirectUri = null)
    {
        var hasToken = await CheckForStoredUser();
        if (hasToken)
            return true;
        if (string.IsNullOrEmpty(redirectUri))
            return false;

        var data = await _session.GetOAuthDataFromRedirect(redirectUri);
        if (data == null) return false;

        var user = new TidalUser(data, _userJsonPath, true);

        ActiveUser = user;
        API.UpdateUser(user);

        await user.GetSession(API);
        await user.WriteToFile();

        return false;
    }

    public async Task<bool> IsLoggedIn()
    {
        if (ActiveUser == null || ActiveUser.SessionID == "")
            return false;

        try
        {
            var res = await API.Call(HttpMethod.Get, $"users/{ActiveUser.UserId}/subscription");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetPkceLoginUrl() => _session.GetPkceLoginUrl();

    private async Task<bool> CheckForStoredUser(bool doPkce = true)
    {
        if (_session.AudioQuality != AudioQuality.HI_RES_LOSSLESS)
            doPkce = false;

        _isPkce = doPkce;

        if (_userJsonPath != null && File.Exists(_userJsonPath))
        {
            try
            {
                var userData = await File.ReadAllTextAsync(_userJsonPath);
                var user = JsonConvert.DeserializeObject<TidalUser>(userData);
                if (user == null) return false;

                user.UpdateJsonPath(_userJsonPath);

                ActiveUser = user;
                API.UpdateUser(user);

                await user.GetSession(API);

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
