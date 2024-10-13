namespace TidalSharp.Data;

internal static class Globals
{
    public const string API_OAUTH2_TOKEN = "https://auth.tidal.com/v1/oauth2/token";
    public const string API_PKCE_AUTH = "https://login.tidal.com/authorize";
    public const string API_V1_LOCATION = "https://api.tidal.com/v1/";
    public const string API_V2_LOCATION = "https://api.tidal.com/v2/";
    public const string OPENAPI_V2_LOCATION = "https://openapi.tidal.com/v2/";

    public const string PKCE_URI_REDIRECT = "https://tidal.com/android/login/auth";

    public const string CLIENT_ID = "zrU4bXHjVVnc.2AtmDPdog4et";
    public const string CLIENT_SECRET = "V\x8cJ\x81K|hyD\x84Fyq\x88J\x81P.qZv\x86s\x85P}V\x80N~BV6ukXTJmlvbttP7wlMlrc72se4=";

    public const string CLIENT_ID_PKCE = "6BDSRdpK9hqEBTgU";
    public const string CLIENT_SECRET_PKCE = "xeuPmY7nbpZ9IIbLAcQ93shka1VNheUAqN6IcszjTG8=";

    public static string GetImageUrl(string hash, MediaResolution res)
        => string.Format(IMAGE_URL_TEMPLATE, hash.Replace('-', '/'), (int)res);

    public static string GetVideoUrl(string hash, MediaResolution res)
        => string.Format(IMAGE_URL_TEMPLATE, hash.Replace('-', '/'), (int)res);

    private const string IMAGE_URL_TEMPLATE = "https://resources.tidal.com/images/{0}/{1}x{1}.jpg";
    private const string VIDEO_URL_TEMPLATE = "https://resources.tidal.com/videos/{0}/{1}x{1}.mp4";
}
