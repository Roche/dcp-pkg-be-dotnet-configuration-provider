namespace OpenDCP.Framework.ConfigurationProvider.Models
{
    public class HeadersDetailsDTO
    {
        public string DCPMicroserviceHeaderName { get; set; }
        public string DCPAPIGatewaySwaggerHeaderName { get; set; }
        public string DCPAPIGatewayHeaderName { get; set; }
        public string CSRFHeaderName { get; set; }
    }
}
