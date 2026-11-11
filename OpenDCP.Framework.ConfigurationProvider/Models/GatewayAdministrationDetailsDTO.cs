namespace OpenDCP.Framework.ConfigurationProvider.Models
{
    public class GatewayAdministrationDetailsDTO
    {
        public string BaseUrl { get; set; }
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
