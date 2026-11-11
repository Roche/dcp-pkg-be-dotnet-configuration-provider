using OpenDCP.Framework.ConfigurationProvider.Classes;
using OpenDCP.Framework.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.ConfigurationProvider.Constants;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Cert;
using VaultSharp.V1.AuthMethods.JWT;
using VaultSharp.V1.AuthMethods.UserPass;
using VaultSharp.V1.Commons;

namespace OpenDCP.Framework.ConfigurationProvider.Providers
{
    public class VaultSecretConfigurationProvider : ISecretConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly IVaultClient _vaultClient;
        private readonly IMemoryCache _memoryCache;

        public VaultSecretConfigurationProvider(IConfiguration configuration, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _vaultClient = GetVaultClient();
            _memoryCache = memoryCache;
        }
        public IConfiguration AddSecretsToConfiguration(IConfiguration configuration, SecretConfigurationProviderOptions options)
        {
            //ToDo add validation if provider can not accessec

            if (options.AddEncryptionKey)
            {
                var key = GetEncryptKeyDetailsAsync();

                configuration["Secrets:EncryptionKey"] = key;
            }

            if (options.AddMessageBusConfiguration)
            {
                if (options.MessageBusType == null)
                {
                    throw new DCPBadRequestException(ErrorMessages.MissingMessageBusType);
                }

                var messageBusConfig = GetMessageBusConfig(MessageBusType.RabbitMQ);

                foreach (var item in messageBusConfig)
                {
                    configuration[$"MessageBusProvider:Settings:{item.Key}"] = item.Value.ToString();
                }
            }

            if (options.AddHeaders)
            {
                var headerDetails = GetAuthorizationHeadersAsync().Result;
                configuration["Headers:Microservice:Value"] = headerDetails.DCPMicroserviceHeaderName;
                configuration["Headers:Gateway:Value"] = headerDetails.DCPAPIGatewayHeaderName;
                configuration["Headers:Swagger:Value"] = headerDetails.DCPAPIGatewaySwaggerHeaderName;
                configuration["Headers:Origin:Value"] = headerDetails.CSRFHeaderName;

                //These places are for legacy reasons, however the places from above should be used
                if (options.AddLegacyHeaders)
                {
                    configuration["DCPMicroserviceHeaderValue"] = headerDetails.DCPMicroserviceHeaderName;
                    configuration["DCPAPIGatewayHeaderValue"] = headerDetails.DCPAPIGatewayHeaderName;
                    configuration["DCPAPIGatewaySwaggerHeaderValue"] = headerDetails.DCPAPIGatewaySwaggerHeaderName;
                    configuration["OriginHeader"] = headerDetails.CSRFHeaderName;
                }
            }

            if (options.AddModuleClient)
            {
                var authorizationServer = GetAuthorizationServerInfoAsync().Result;
                configuration["IdP:Settings:Client:Id"] = authorizationServer.ClientId;
                configuration["IdP:Settings:Client:Secret"] = authorizationServer.ClientSecret;
            }

            if (options.AddGatewayClient)
            {
                var gateway = GetGatewayAdministrationDetails();
                configuration["IdP:Settings:GatewayClient:Id"] = gateway.ClientId;
                configuration["IdP:Settings:GatewayClient:Secret"] = gateway.ClientSecret;
            }

            if (options.AddAudience)
            {
                var audience = GetAudience();
                configuration["IdP:Settings:Audience:Id"] = audience.AudienceId;
                configuration["IdP:Settings:Audience:Secret"] = audience.AudienceSecret;
            }

            if (options.AddLDAP)
            {
                var ldapDetails = GetActiveDirectoryDetailsAsync();

                configuration["IdP:Settings:User"] = ldapDetails.User;
                configuration["IdP:Settings:Password"] = ldapDetails.Password;
            }

            if (options.AddExternalDatasourceConnectionStringPattern)
            {
                var builtExternalConnectionString = BuildExternalConnectionString();

                configuration["Secrets:ExternalDatasourceConnectionStringPattern"] = builtExternalConnectionString;
            }

            return configuration;
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
                var databaseCredentials = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:Database"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (databaseCredentials.Data.Data.TryGetValue("Server", out object dataSource)
                    && databaseCredentials.Data.Data.TryGetValue("DatabaseName", out object databaseName)
                    && databaseCredentials.Data.Data.TryGetValue("UserId", out object userId)
                    && databaseCredentials.Data.Data.TryGetValue("Password", out object password))
                {

                    if (databaseType.Equals("MSSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        result = $"Server={dataSource};Database={databaseName};User Id={userId};password='{password}';Trusted_Connection=false;Encrypt=false;MultipleActiveResultSets=true;";
                    }
                    else if (databaseType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        result = $"Host={dataSource};" +
                            $"Database={databaseName};" +
                            $"Username={userId};" +
                            $"Password='{password}';" +
                            $"Maximum Pool Size=50;" +
                            $"Minimum Pool Size=5;" +
                            $"Timeout=15;";
                    }
                    else
                    {
                        throw new Exception("Unsupported database type.");
                    }

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(
                            "connectionStringDetails",
                            result,
                            TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:ConnectionString"]))
                         );
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.VaultDbCredentialsNotDefined);
                }
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
                var audienceDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:Token"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (audienceDetails.Data.Data.TryGetValue("AudienceId", out object audienceId)
                 && audienceDetails.Data.Data.TryGetValue("AudienceSecret", out object audienceSecret))
                {
                    result = new AudienceDTO
                    {
                        AudienceId = audienceId.ToString(),
                        AudienceSecret = audienceSecret.ToString()
                    };

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(
                            key: "audienceDetails",
                            value: result,
                            absoluteExpirationRelativeToNow: TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:Audience"]))
                           );
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.TokenDetailsNotDefined);
                }
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
                var headersDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:Headers"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                var microserviceHeaderName = _configuration["Headers:Microservice:Name"] ?? _configuration["DCPMicroserviceHeaderName"];
                var apiGatewaySwaggerHeaderName = _configuration["Headers:Swagger:Name"] ?? _configuration["DCPAPIGatewaySwaggerHeaderName"];
                var apiGatewayHeaderName = _configuration["Headers:Gateway:Name"] ?? _configuration["DCPAPIGatewayHeaderName"];
                var CSRFHeaderName = _configuration["Headers:CSRF:Name"] ?? _configuration["CSRFHeaderName"];

                if (headersDetails.Data.Data.TryGetValue(microserviceHeaderName, out object dcpMicroserviceHeaderName)
                     && headersDetails.Data.Data.TryGetValue(apiGatewaySwaggerHeaderName, out object dcpAPIGatewaySwaggerHeaderName)
                     && headersDetails.Data.Data.TryGetValue(apiGatewayHeaderName, out object dcpAPIGatewayHeaderName)
                     && headersDetails.Data.Data.TryGetValue(CSRFHeaderName, out object piCSRFHeaderValue))
                {
                    result = new HeadersDetailsDTO
                    {
                        DCPMicroserviceHeaderName = dcpMicroserviceHeaderName.ToString(),
                        DCPAPIGatewaySwaggerHeaderName = dcpAPIGatewaySwaggerHeaderName.ToString(),
                        DCPAPIGatewayHeaderName = dcpAPIGatewayHeaderName.ToString(),
                        CSRFHeaderName = piCSRFHeaderValue.ToString()
                    };

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set("headersDetails", result, TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:Headers"])));
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.HeadersDetailsNotDefined);
                }
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
                var clientDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:Client"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (clientDetails.Data.Data.TryGetValue("ClientId", out object clientId)
                    && clientDetails.Data.Data.TryGetValue("ClientSecret", out object clientSecret))
                {
                    result = new AuthorizationServerDTO
                    {
                        ClientId = clientId.ToString(),
                        ClientSecret = clientSecret.ToString()
                    };

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(
                            key: "authorizationServerDetails",
                            value: result,
                            TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:AuthorizationServer"]))
                          );
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.ClientDetailsNotDefined);
                }
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
                var clientDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:Gateway"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (clientDetails.Data.Data.TryGetValue("AdministrationClientId", out object clientId)
                && clientDetails.Data.Data.TryGetValue("AdministrationClientSecret", out object clientSecret))
                {
                    result = new GatewayAdministrationDetailsDTO
                    {
                        BaseUrl = _configuration["DCPCoreBaseUrl"],
                        TokenEndpoint = _configuration["DCPCoreTokenEndPoint"],
                        ClientId = clientId.ToString(),
                        ClientSecret = clientSecret.ToString()
                    };

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set("gatewayAdministrationDetails", result, TimeSpan.FromDays(1));
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.ClientDetailsNotDefined);
                }
            }

            return result;
        }
        public string GetEncryptKeyDetailsAsync()
        {
            string result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<string>("encryptKeyDetails");
            }

            if (result is null)
            {
                var encryptKeyDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:EncryptKey"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (encryptKeyDetails.Data.Data.TryGetValue("EncryptKey", out object encryptKey))
                {
                    result = encryptKey.ToString();

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(
                            "encryptKeyDetails",
                            result,
                            TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:EncryptKey"]))
                          );
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.EncryptKeyDetailsNotDefined);
                }
            }

            return result;


        }
        public ActiveDirectoryDTO GetActiveDirectoryDetailsAsync()
        {
            ActiveDirectoryDTO result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<ActiveDirectoryDTO>("activeDirectoryDetails");
            }

            if (result is null)
            {
                var activeDirectoryDetails = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:LDAP"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                if (activeDirectoryDetails.Data.Data.TryGetValue("User", out object user)
                  && activeDirectoryDetails.Data.Data.TryGetValue("Password", out object password))
                {
                    result = new ActiveDirectoryDTO
                    {
                        User = user.ToString(),
                        Password = password.ToString()
                    };

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(
                            "activeDirectoryDetails",
                            result,
                            TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:ActiveDirectory"]))
                         );
                    }
                }
                else
                {
                    throw new DCPBadRequestException(ErrorMessages.ClientDetailsNotDefined);
                }
            }

            return result;
        }
        public async Task SaveSecretAsync(string path, Dictionary<string, object> secret)
        {
            await SaveSecretInternalAsync(path, secret, null, _configuration["SecretConfigurationProvider:Settings:MountPoints:DataSource"]);
        }
        public JsonObject GetMessageBusConfig(MessageBusType type)
        {
            var result = new JsonObject();

            if (type == MessageBusType.RabbitMQ)
            {
                JsonObject rabbitMQMessageBusDetails = null;

                if (_memoryCache != null)
                {
                    rabbitMQMessageBusDetails = _memoryCache.Get<JsonObject>("rabbitMQMessageBusDetails");
                }

                if (rabbitMQMessageBusDetails is null)
                {
                    var rabbitmqCredentials = ReadSecret(_configuration["SecretConfigurationProvider:Settings:Paths:MessageBus"], _configuration["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]);

                    if (rabbitmqCredentials.Data.Data.TryGetValue("CertPassphrase", out object certPassphrase)
                        && rabbitmqCredentials.Data.Data.TryGetValue("Host", out object host)
                        && rabbitmqCredentials.Data.Data.TryGetValue("UserName", out object userName)
                        && rabbitmqCredentials.Data.Data.TryGetValue("Password", out object password)
                        && rabbitmqCredentials.Data.Data.TryGetValue("Port", out object port)
                        && rabbitmqCredentials.Data.Data.TryGetValue("UseSSL", out object useSSL))
                    {
                        rabbitMQMessageBusDetails = new JsonObject()
                        {
                            ["Host"] = JsonValue.Create(host.ToString()),
                            ["Port"] = JsonValue.Create(int.Parse(port.ToString())),
                            ["User"] = JsonValue.Create(userName.ToString()),
                            ["SslCertName"] = JsonValue.Create(_configuration["MessageBusProvider:Settings:SslCertName"]),
                            ["SslCertPasspharse"] = JsonValue.Create(certPassphrase.ToString()),
                            ["Password"] = JsonValue.Create(password.ToString()),
                            ["UseSSL"] = JsonValue.Create(bool.Parse(useSSL.ToString())),
                        };

                        if (_memoryCache != null)
                        {
                            _memoryCache.Set(
                                "rabbitMQMessageBusDetails",
                                rabbitMQMessageBusDetails,
                                TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:MessageBus"]))
                              );
                        }

                        result = rabbitMQMessageBusDetails;
                    }
                    else
                    {
                        throw new DCPInternalServerErrorException(ErrorMessages.InvalidMessageBusConfig);
                    }
                }
                else
                {
                    result = rabbitMQMessageBusDetails;
                }
            }
            else
            {
                throw new DCPInternalServerErrorException(ErrorMessages.UnkownMessageBusType);
            }

            return result;
        }
        public JsonObject GetSecretAsJSON(string path, string mountPoints)
        {
            JsonObject result = null;

            if (_memoryCache != null)
            {
                result = _memoryCache.Get<JsonObject>("secretJsonDetails");
            }

            if (result is null)
            {
                var secretJsonDetails = ReadSecret(path, mountPoints);

                var secretData = secretJsonDetails.Data.Data;

                string jsonString = JsonSerializer.Serialize(secretData);

                result = JsonNode.Parse(jsonString)?.AsObject();

                if (_memoryCache != null)
                {
                    _memoryCache.Set(
                        "secretJsonDetails",
                        result,
                        TimeSpan.FromDays(double.Parse(_configuration["MemoryCache:TTL:SecretJson"]))
                     );
                }
            }

            return result;
        }
        private IVaultClient GetVaultClient()
        {
            var vaultClientSettings = new VaultClientSettings(_configuration["SecretConfigurationProvider:Settings:Address"], GetAuthMethod())
            {
                Namespace = _configuration["SecretConfigurationProvider:Settings:Namespace"],
                ContinueAsyncTasksOnCapturedContext = false
            };

            return new VaultClient(vaultClientSettings);
        }
        private IAuthMethodInfo GetAuthMethod()
        {
            IAuthMethodInfo authMethod = null;

            var authMethodName = _configuration["SecretConfigurationProvider:Settings:Auth:Method"];

            if (string.IsNullOrEmpty(authMethodName))
            {
                throw new DCPInternalServerErrorException(ErrorMessages.UnkownAuthMethod);
            }

            if (authMethodName == HashiCorpVaultAuthMethods.Certificate)
            {
                var certReader = new CertificateReader();
                var cers = certReader.FindCertificateByName(_configuration["SecretConfigurationProvider:Settings:Auth:CertificateName"]);

                if (cers.Count == 0)
                {
                    throw new DCPInternalServerErrorException(ErrorMessages.CertificateNotFound);
                }

                authMethod = new CertAuthMethodInfo(cers);
            }
            else if (authMethodName == HashiCorpVaultAuthMethods.AppRole)
            {
                authMethod = new AppRoleAuthMethodInfo(
                    _configuration["SecretConfigurationProvider:Settings:Auth:AppRole"],
                    _configuration["SecretConfigurationProvider:Settings:Auth:Secret"]
                   );
            }
            else if (authMethodName == HashiCorpVaultAuthMethods.UserPass)
            {
                authMethod = new UserPassAuthMethodInfo(
                    _configuration["SecretConfigurationProvider:Settings:Auth:User"],
                    _configuration["SecretConfigurationProvider:Settings:Auth:Password"]
                );
            }
            else if (authMethodName == HashiCorpVaultAuthMethods.Jwt)
            {
                authMethod = new JWTAuthMethodInfo(
                    _configuration["SecretConfigurationProvider:Settings:Auth:Role"],
                    _configuration["SecretConfigurationProvider:Settings:Auth:Jwt"]
                );
            }
            else
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownAuthMethod);
            }

            return authMethod;
        }

        private Secret<SecretData> ReadSecret(string path, string mountPoint)
        {
            var secret = new Secret<SecretData>();

            try
            {
                secret = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, null, mountPoint).Result;
            }
            catch (VaultApiException)
            {
                _vaultClient.V1.Auth.ResetVaultToken();
                secret = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, null, mountPoint).Result;
            }
            catch (AggregateException exception)
            {
                if (exception.InnerExceptions.Any(x => x is VaultApiException || x is SocketException || x is HttpRequestException))
                {
                    _vaultClient.V1.Auth.ResetVaultToken();
                    secret = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, null, mountPoint).Result;
                }
                else
                {
                    throw exception;
                }
            }

            return secret;
        }

        private async Task SaveSecretInternalAsync(string path, Dictionary<string, object> data, int? checkAndSet = null, string mountPoint = null)
        {
            try
            {
                await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data, checkAndSet, mountPoint);
            }
            catch (VaultApiException)
            {
                _vaultClient.V1.Auth.ResetVaultToken();
                await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data, checkAndSet, mountPoint);
            }
            catch (AggregateException exception)
            {
                if (exception.InnerExceptions.Any(x => x is VaultApiException || x is SocketException || x is HttpRequestException))
                {
                    _vaultClient.V1.Auth.ResetVaultToken();
                    await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data, checkAndSet, mountPoint);
                }
                else
                {
                    throw exception;
                }
            }
        }

        public string BuildExternalConnectionString()
        {
            var vaultDataSourceMountPoint = _configuration["SecretConfigurationProvider:Settings:MountPoints:ExternalDatasource"];
            var vaultDataSourcePath = _configuration["SecretConfigurationProvider:Settings:Paths:ExternalDatasource"];

            return "vault::" + vaultDataSourceMountPoint + "data" + "/" + vaultDataSourcePath + "/{{datasourceId}}";
        }
    }
}
