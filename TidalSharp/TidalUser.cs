using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TidalSharp.Data;

namespace TidalSharp;

[JsonObject(MemberSerialization.OptIn)]
public class TidalUser
{
    [JsonConstructor]
    internal TidalUser(OAuthTokenData data, bool isPkce)
    {
        _data = data;
        IsPkce = isPkce;
        
        DateTime now = DateTime.UtcNow;
        ExpirationDate = now.AddSeconds(data.ExpiresIn);
    }

    internal async Task GetSession(HttpClient httpClient)
    {
        // TODO: transition this to use a standard Requests class like in tidalapi to allow for auto-token refreshing
        HttpRequestMessage request = new()
        {
            RequestUri = new(Globals.API_V1_LOCATION + "sessions"),
            Method = HttpMethod.Get,
        };
        request.Headers.Add("Authorization", $"{_data.TokenType} {_data.AccessToken}");

        HttpResponseMessage response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Session request failed: {await response.Content.ReadAsStringAsync()}");

        try
        {
            _sessionInfo = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<SessionInfo>();
        }
        catch
        {
            throw new Exception("Invalid response for session info.");
        }
    }

    [JsonProperty("Data")]
    private OAuthTokenData _data;
    private SessionInfo? _sessionInfo;


    [JsonProperty("IsPkce")]
    public bool IsPkce { get; init; }

    public string AccessToken => _data.AccessToken;
    public string RefreshToken => _data.RefreshToken;
    public string TokenType => _data.TokenType;
    public DateTime ExpirationDate { get; init; }

    public string CountryCode => _sessionInfo?.CountryCode ?? "";
    public string SessionID => _sessionInfo?.SessionId ?? "";
}
