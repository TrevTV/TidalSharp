using Newtonsoft.Json;

namespace TidalSharp.Data;

public class OAuthTokenData
{
    [JsonProperty("scope")]
    public string Scope { get; set; }

    [JsonProperty("user")]
    public UserData User { get; set; }

    [JsonProperty("clientName")]
    public string ClientName { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    public class UserData
    {
        [JsonProperty("userId")]
        public long UserId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("fullName")]
        public object FullName { get; set; }

        [JsonProperty("firstName")]
        public object FirstName { get; set; }

        [JsonProperty("lastName")]
        public object LastName { get; set; }

        [JsonProperty("nickname")]
        public object Nickname { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("address")]
        public object Address { get; set; }

        [JsonProperty("city")]
        public object City { get; set; }

        [JsonProperty("postalcode")]
        public object Postalcode { get; set; }

        [JsonProperty("usState")]
        public object UsState { get; set; }

        [JsonProperty("phoneNumber")]
        public object PhoneNumber { get; set; }

        [JsonProperty("birthday")]
        public object Birthday { get; set; }

        [JsonProperty("channelId")]
        public long ChannelId { get; set; }

        [JsonProperty("parentId")]
        public long ParentId { get; set; }

        [JsonProperty("acceptedEULA")]
        public bool AcceptedEula { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("updated")]
        public long Updated { get; set; }

        [JsonProperty("facebookUid")]
        public long FacebookUid { get; set; }

        [JsonProperty("appleUid")]
        public object AppleUid { get; set; }

        [JsonProperty("googleUid")]
        public object GoogleUid { get; set; }

        [JsonProperty("accountLinkCreated")]
        public bool AccountLinkCreated { get; set; }

        [JsonProperty("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("newUser")]
        public bool NewUser { get; set; }
    }
}