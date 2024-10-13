using Newtonsoft.Json;
using TidalSharp.Data;

namespace TidalSharp;

public class TidalClient
{
    public TidalClient(string? dataPath = null)
    {
        _dataPath = dataPath;
        _userJsonPath = _dataPath == null ? null : Path.Combine(_dataPath, "lastUser.json");

        if (_dataPath != null && !Directory.Exists(_dataPath))
            Directory.CreateDirectory(_dataPath);

        _httpClientHandler = new() { CookieContainer = new() };
        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 12; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("X-Tidal-Token", Globals.CLIENT_ID);

        // TODO: lazy defaults
        Session = new(_httpClient, AudioQuality.HIGH, VideoQuality.HIGH);
        API = new(_httpClient, Session);
    }

    public Session Session { get; init; }
    public API API { get; init; }

    private TidalUser? _activeUser;
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

        var data = await Session.GetOAuthDataFromRedirect(redirectUri);
        if (data == null) return false;

        var user = new TidalUser(data, _userJsonPath, true);
        await user.GetSession(API);
        await user.WriteToFile();

        _activeUser = user;
        API.UpdateUser(user);

        return false;
    }

    public async Task<bool> IsLoggedIn()
    {
        if (_activeUser == null || _activeUser.SessionID == "")
            return false;

        try
        {
            var res = await API.Call(HttpMethod.Get, $"users/{_activeUser.UserId}/subscription");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckForStoredUser(bool doPkce = true)
    {
        if (Session.AudioQuality != AudioQuality.HI_RES_LOSSLESS)
            doPkce = false;

        _isPkce = doPkce;

        if (_userJsonPath != null && File.Exists(_userJsonPath))
        {
            try
            {
                var userData = await File.ReadAllTextAsync(_userJsonPath);
                var user = JsonConvert.DeserializeObject<TidalUser>(userData);
                if (user == null) return false;

                await user.GetSession(API);

                _activeUser = user;
                API.UpdateUser(user);

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
