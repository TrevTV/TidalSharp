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
        JObject result = await api.Call(HttpMethod.Get, "sessions", null, null, new() { { "Authorization", $"{_data.TokenType} {_data.AccessToken}" } });

        try
        {
            _sessionInfo = result.ToObject<SessionInfo>();
        }
        catch
        {
            throw new Exception("Invalid response for session info.");
        }
    }

    internal async Task UpdateOAuthTokenData(OAuthTokenData data)
    {
        // TODO: i am unsure if a Session update is needed here
        _data = data;
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
    public DateTime ExpirationDate { get; init; }

    public long UserId => _data.UserId;
    public string CountryCode => _sessionInfo?.CountryCode ?? "";
    public string SessionID => _sessionInfo?.SessionId ?? "";
}
