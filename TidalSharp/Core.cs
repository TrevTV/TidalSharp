namespace TidalSharp;

public class TidalClient
{
    public TidalClient()
    {
        _clientHandler = new() { CookieContainer = new() };
        _client = new HttpClient(_clientHandler);
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 12; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/91.0.4472.114 Safari/537.36");

        // TODO: lazy defaults
        Session = new(_client, Data.AudioQuality.HIGH, Data.VideoQuality.HIGH);
        LoginToken();
    }

    public Session Session { get; init; }

    private HttpClient _client;
    private HttpClientHandler _clientHandler;

    private bool _isPkce;

    public bool Login()
    {
        var hasToken = LoginToken();
        if (hasToken)
            return true;

        var pkceUrl = Session.GetPkceLoginUrl();
        Console.WriteLine(pkceUrl);

        // TODO: login_pkce
        return false;
    }

    private bool LoginToken(bool doPkce = true)
    {
        if (Session.AudioQuality != Data.AudioQuality.HI_RES_LOSSLESS)
            doPkce = false;

        _isPkce = doPkce;

        // TODO: load token from storage

        return false;
    }
}
