using TidalSharp.Data;

namespace TidalSharp;

public class TidalClient
{
    public TidalClient()
    {
        _httpClientHandler = new() { CookieContainer = new() };
        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 12; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("X-Tidal-Token", Globals.CLIENT_ID);

        // TODO: lazy defaults
        Session = new(_httpClient, AudioQuality.HIGH, VideoQuality.HIGH);
        LoginToken();
    }

    public Session Session { get; init; }

    private TidalUser? _activeUser;
    private bool _isPkce;

    private HttpClient _httpClient;
    private HttpClientHandler _httpClientHandler;

    public async Task<bool> Login(string? redirectUri = null)
    {
        var hasToken = LoginToken();
        if (hasToken)
            return true;
        if (string.IsNullOrEmpty(redirectUri))
            return false;

        var data = await Session.GetOAuthDataFromRedirect(redirectUri);
        if (data == null) return false;

        var session = await Session.GetSessionInfo(data);
        if (session == null) return false;


        var user = new TidalUser(data, session, true);
        _activeUser = user;
        Session.UpdateUser(user);

        return false;
    }

    private bool LoginToken(bool doPkce = true)
    {
        if (Session.AudioQuality != AudioQuality.HI_RES_LOSSLESS)
            doPkce = false;

        _isPkce = doPkce;

        // TODO: load token from storage

        return false;
    }
}
