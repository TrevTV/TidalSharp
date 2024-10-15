using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TidalSharp.Data;
using TidalSharp.Exceptions;

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

    internal async Task GetSession(API api, CancellationToken token = default)
    {
        JObject result = await api.Call(HttpMethod.Get, "sessions", token: token);

        try
        {
            _sessionInfo = result.ToObject<SessionInfo>();
        }
        catch
        {
            throw new APIException("Invalid response for session info.");
        }
    }

    internal async Task RefreshOAuthTokenData(OAuthTokenData data, CancellationToken token = default)
    {
        if (_data == null)
            throw new InvalidOperationException("Attempting to refresh a user with no existing data.");

        _data.AccessToken = data.AccessToken;
        _data.ExpiresIn = data.ExpiresIn;

        DateTime now = DateTime.UtcNow;
        ExpirationDate = now.AddSeconds(data.ExpiresIn);

        await WriteToFile(token);
    }

    internal void UpdateJsonPath(string? jsonPath) => _jsonPath = jsonPath;

    internal async Task WriteToFile(CancellationToken token = default)
    {
        if (_jsonPath != null)
            await File.WriteAllTextAsync(_jsonPath, JsonConvert.SerializeObject(this), token);
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
