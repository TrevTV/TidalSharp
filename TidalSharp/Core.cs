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
    }

    public Session Session { get; init; }

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

        var user = new TidalUser(data, true);
        await user.GetSession(_httpClient);

        if (_dataPath != null && _userJsonPath != null)
            await File.WriteAllTextAsync(_userJsonPath, JsonConvert.SerializeObject(user));

        _activeUser = user;
        Session.UpdateUser(user);

        return false;
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

                await user.GetSession(_httpClient);

                _activeUser = user;
                Session.UpdateUser(user);

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
