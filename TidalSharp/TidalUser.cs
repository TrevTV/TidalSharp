using Newtonsoft.Json;
using TidalSharp.Data;

namespace TidalSharp;

[JsonObject(MemberSerialization.OptIn)]
public class TidalUser
{
    [JsonConstructor]
    internal TidalUser(OAuthTokenData data, SessionInfo sessionInfo, bool isPkce)
    {
        _data = data;
        _sessionInfo = sessionInfo;
        IsPkce = isPkce;
        
        DateTime now = DateTime.UtcNow;
        ExpirationDate = now.AddSeconds(data.ExpiresIn);
    }

    [JsonProperty("Data")] private OAuthTokenData _data;
    [JsonProperty("SessionInfo")] private SessionInfo _sessionInfo;

    public string AccessToken => _data.AccessToken;
    public string RefreshToken => _data.RefreshToken;
    public string TokenType => _data.TokenType;
    public DateTime ExpirationDate { get; init; }

    [JsonProperty("IsPkce")] public bool IsPkce { get; init; }

    public string CountryCode => _sessionInfo.CountryCode;
    public string SessionID => _sessionInfo.SessionId;
}
