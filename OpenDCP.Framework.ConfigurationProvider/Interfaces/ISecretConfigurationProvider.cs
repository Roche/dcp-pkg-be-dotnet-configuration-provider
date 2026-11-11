using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.ConfigurationProvider.Constants;
using OpenDCP.Framework.ConfigurationProvider.Models;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenDCP.Framework.ConfigurationProvider.Interfaces
{
    public interface ISecretConfigurationProvider
    {
        IConfiguration AddSecretsToConfiguration(IConfiguration configuration, SecretConfigurationProviderOptions options);
        JsonObject GetMessageBusConfig(MessageBusType type);
        Task<AuthorizationServerDTO> GetAuthorizationServerInfoAsync();
        Task<HeadersDetailsDTO> GetAuthorizationHeadersAsync();
        AudienceDTO GetAudience();
        string GetConnectionString(string databaseType = "MSSQL");
        GatewayAdministrationDetailsDTO GetGatewayAdministrationDetails();
        string GetEncryptKeyDetailsAsync();
        ActiveDirectoryDTO GetActiveDirectoryDetailsAsync();
        Task SaveSecretAsync(string path, Dictionary<string, object> secret);
        string BuildExternalConnectionString();
        JsonObject GetSecretAsJSON(string path, string mountPoints);
    }
}
