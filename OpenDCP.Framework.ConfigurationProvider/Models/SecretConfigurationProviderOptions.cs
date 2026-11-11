using OpenDCP.Framework.ConfigurationProvider.Constants;

namespace OpenDCP.Framework.ConfigurationProvider.Models
{
    public class SecretConfigurationProviderOptions
    {
        public bool AddMessageBusConfiguration { get; set; } = false;
        public MessageBusType? MessageBusType { get; set; } = null;
        public bool AddHeaders { get; set; } = true;
        public bool AddLegacyHeaders { get; set; } = false;
        public bool AddModuleClient { get; set; } = false;
        public bool AddGatewayClient { get; set; } = false;
        public bool AddAudience { get; set; } = false;
        public bool AddLDAP { get; set; } = false;
        public bool AddEncryptionKey { get; set; } = false;
        public bool AddExternalDatasourceConnectionStringPattern { get; set; } = true;
    }
}
