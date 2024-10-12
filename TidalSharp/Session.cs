using System.Security.Cryptography;
using System.Text;
using System.Web;
using TidalSharp.Data;

namespace TidalSharp;

public class Session
{
    public Session(AudioQuality audioQuality, VideoQuality videoQuality, int itemLimit = 1000, bool alac = true)
    {
        AudioQuality = audioQuality;
        VideoQuality = videoQuality;
        _alac = alac;

        _itemLimit = itemLimit > 10000 ? 10000 : itemLimit;

        _clientUniqueKey = $"{BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0):x}";
        _codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=');

        using var sha256 = SHA256.Create();
        _codeChallenge = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(_codeVerifier))).TrimEnd('=');
    }

    public AudioQuality AudioQuality { get; init; }
    public VideoQuality VideoQuality { get; init; }

    private int _itemLimit;
    private bool _alac;

    private string _clientUniqueKey;
    private string _codeVerifier;
    private string _codeChallenge;

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
}
