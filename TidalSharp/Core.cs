namespace TidalSharp;

public class TidalClient
{
    public TidalClient()
    {
        // TODO: lazy defaults
        _session = new(Data.AudioQuality.HIGH, Data.VideoQuality.HIGH);
        LoginToken();
    }

    private Session _session;
    private bool _isPkce;

    public bool Login()
    {
        var hasToken = LoginToken();
        if (hasToken)
            return true;

        var pkceUrl = _session.GetPkceLoginUrl();
        Console.WriteLine(pkceUrl);

        // TODO: login_pkce
        return false;
    }

    private bool LoginToken(bool doPkce = true)
    {
        if (_session.AudioQuality != Data.AudioQuality.HI_RES_LOSSLESS)
            doPkce = false;

        _isPkce = doPkce;

        // TODO: load token from storage

        return false;
    }
}
