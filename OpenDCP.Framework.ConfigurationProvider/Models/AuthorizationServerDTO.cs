namespace OpenDCP.Framework.ConfigurationProvider.Models
{
    public class AuthorizationServerDTO
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string BaseUrl { get; set; }
        public string TokenEndpoint { get; set; }
    }
}
