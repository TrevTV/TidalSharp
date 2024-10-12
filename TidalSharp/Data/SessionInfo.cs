using Newtonsoft.Json;

namespace TidalSharp.Data;

public class SessionInfo
{
    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("countryCode")]
    public string CountryCode { get; set; }

    [JsonProperty("channelId")]
    public long ChannelId { get; set; }

    [JsonProperty("partnerId")]
    public long PartnerId { get; set; }

    [JsonProperty("client")]
    public ClientData Client { get; set; }

    public class ClientData
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("authorizedForOffline")]
        public bool AuthorizedForOffline { get; set; }
    }
}