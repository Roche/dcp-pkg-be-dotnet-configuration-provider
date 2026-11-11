using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Providers;
using System;

namespace OpenDCP.Framework.ConfigurationProvider.Factories
{
    public class SecretConfigurationProviderFactory
    {
        public ISecretConfigurationProvider Load(string driver, IMemoryCache memoryCache, IConfiguration configuration)
        {
            switch (driver.ToLower())
            {
                case "hashicorpvault":
                    return new VaultSecretConfigurationProvider(configuration, memoryCache);
                case "file":
                    return new FileSecretConfigurationProvider(configuration, memoryCache);
                default:
                    throw new Exception($"The driver {driver} is not known as a secret provider");
            }
        }
    }
}
