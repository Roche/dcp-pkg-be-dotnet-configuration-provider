using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.ConfigurationProvider.Constants;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenDCP.Framework.ConfigurationProvider.Providers
{
    public class FileSecretConfigurationProvider : ISecretConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        public IConfiguration AddSecretsToConfiguration(IConfiguration configuration, SecretConfigurationProviderOptions options)
        {
            throw new NotImplementedException();
        }

        public FileSecretConfigurationProvider(IConfiguration configuration, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
        }
        public string GetConnectionString(string databaseType = "MSSQL")
        {
            string result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<string>("connectionStringDetails");
            }

            if (result is null)
            {
                return _configuration[$"ConnectionString"];
            }

            return result;
        }

        public AudienceDTO GetAudience()
        {
            AudienceDTO result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<AudienceDTO>("audienceDetails");
            }

            if (result is null)
            {
                result = new AudienceDTO
                {
                    AudienceId = _configuration["Audience:Id"],
                    AudienceSecret = _configuration["Audience:Secret"]
                };
            }

            return result;
        }

        public async Task<HeadersDetailsDTO> GetAuthorizationHeadersAsync()
        {
            HeadersDetailsDTO result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<HeadersDetailsDTO>("headersDetails");
            }

            if (result is null)
            {
                result = new HeadersDetailsDTO
                {
                    CSRFHeaderName = _configuration["Headers:CSFR"],
                    DCPAPIGatewayHeaderName = _configuration["Headers:Gateway"],
                    DCPMicroserviceHeaderName = _configuration["Headers:Microservice"],
                };
            }

            return result;
        }

        public async Task<AuthorizationServerDTO> GetAuthorizationServerInfoAsync()
        {
            AuthorizationServerDTO result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<AuthorizationServerDTO>("authorizationServerDetails");
            }

            if (result is null)
            {
                result = new AuthorizationServerDTO
                {
                    BaseUrl = _configuration["AuthorizationServer:BaseUrl"],
                    ClientId = _configuration["AuthorizationServer:ClientId"],
                    ClientSecret = _configuration["AuthorizationServer:ClientSecret"],
                    TokenEndpoint = _configuration["AuthorizationServer:TokenEndpoint"]
                };
            }

            return result;
        }
        public GatewayAdministrationDetailsDTO GetGatewayAdministrationDetails()
        {
            GatewayAdministrationDetailsDTO result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<GatewayAdministrationDetailsDTO>("gatewayAdministrationDetails");
            }

            if (result is null)
            {
                result = new GatewayAdministrationDetailsDTO
                {
                    BaseUrl = _configuration["GatewayAdministration:BaseUrl"],
                    ClientId = _configuration["GatewayAdministration:ClientId"],
                    ClientSecret = _configuration["GatewayAdministration:ClientSecret"],
                    TokenEndpoint = _configuration["GatewayAdministration:TokenEndpoint"]
                };
            }

            return result;
        }

        public JsonObject GetMessageBusConfig(MessageBusType type)
        {
            var result = new JsonObject();

            /*
             * TODo
            if (type == (byte)MessageBusType.RabbitMQ)
            {
                RabbitMQMessageBusConfig rabbitMQMessageBusDetails = null;

                if (_memoryCache != null)
                {
                    rabbitMQMessageBusDetails = _memoryCache.Get<RabbitMQMessageBusConfig>("rabbitMQMessageBusDetails");
                }

                if (rabbitMQMessageBusDetails is null)
                {
                    var config = new RabbitMQMessageBusConfig
                    {
                        Host = _configuration["RabbitMQ:Host"],
                        Password = _configuration["RabbitMQ:Password"],
                        User = _configuration["RabbitMQ:User"],
                        Port = int.Parse(_configuration["RabbitMQ:Port"]),
                        UseSSL = bool.Parse(_configuration["RabbitMQ:SSL:Enable"]),
                        SslCertName = _configuration["RabbitMQ:SSL:Certname"],
                        SslCertPasspharse = _configuration["RabbitMQ:SSL:Passphrase"]
                    };

                    result = JObject.FromObject(config);
                }
                else
                {
                    result = JObject.FromObject(rabbitMQMessageBusDetails);
                }
            }
            else
            {
                throw new DCPInternalServerErrorException(ErrorMessages.UnkownMessageBusType);
            }

            return result;
            */

            return result;
        }

        public string GetEncryptKeyDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public ActiveDirectoryDTO GetActiveDirectoryDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public Task SaveSecretAsync(string path, Dictionary<string, object> secret)
        {
            throw new NotImplementedException();
        }

        public string BuildExternalConnectionString()
        {
            throw new NotImplementedException();
        }

        public JsonObject GetSecretAsJSON(string path, string mountPoints)
        {
            throw new NotImplementedException();
        }
    }

}
