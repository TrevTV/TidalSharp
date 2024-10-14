using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TidalSharp.Data;

namespace TidalSharp;

[JsonObject(MemberSerialization.OptIn)]
public class TidalUser
{
    [JsonConstructor]
    internal TidalUser(OAuthTokenData data, string? jsonPath, bool isPkce)
    {
        _data = data;
        _jsonPath = jsonPath;
        IsPkce = isPkce;
        
        DateTime now = DateTime.UtcNow;
        ExpirationDate = now.AddSeconds(data.ExpiresIn);
    }

    internal async Task GetSession(API api)
    {
        JObject result = await api.Call(HttpMethod.Get, "sessions");

        try
        {
            _sessionInfo = result.ToObject<SessionInfo>();
        }
        catch
        {
            throw new Exception("Invalid response for session info.");
        }
    }

    internal async Task RefreshOAuthTokenData(OAuthTokenData data)
    {
        if (_data == null)
            throw new Exception("Attempting to refresh a user with no existing data.");

        _data.AccessToken = data.AccessToken;
        _data.ExpiresIn = data.ExpiresIn;

        DateTime now = DateTime.UtcNow;
        ExpirationDate = now.AddSeconds(data.ExpiresIn);

        await WriteToFile();
    }

    internal async Task WriteToFile()
    {
        if (_jsonPath != null)
            await File.WriteAllTextAsync(_jsonPath, JsonConvert.SerializeObject(this));
    }

    private string? _jsonPath;

    [JsonProperty("Data")]
    private OAuthTokenData _data;
    private SessionInfo? _sessionInfo;

    [JsonProperty("IsPkce")]
    public bool IsPkce { get; init; }

    public string AccessToken => _data.AccessToken;
    public string RefreshToken => _data.RefreshToken;
    public string TokenType => _data.TokenType;
    public DateTime ExpirationDate { get; private set; }

    public long UserId => _data.UserId;
    public string CountryCode => _sessionInfo?.CountryCode ?? "";
    public string SessionID => _sessionInfo?.SessionId ?? "";
}
