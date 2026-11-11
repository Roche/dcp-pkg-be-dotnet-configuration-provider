namespace OpenDCP.Framework.ConfigurationProvider.Constants
{
    internal static class ErrorMessages
    {
        internal static string UnkownMessageBusType = "Unkown Message Bus Type";
        internal static string MissingMessageBusType = "Message Bus Type configuration is required but missing";
        internal static string VaultDbCredentialsNotDefined = "Vault database credentials not defined";
        internal const string TokenDetailsNotDefined = "Missing token details";
        internal const string HeadersDetailsNotDefined = "Missing headers details";
        internal const string ClientDetailsNotDefined = "Missing client details";
        internal const string EncryptKeyDetailsNotDefined = "Missing encryptKey details";
        internal const string UnkownAuthMethod = "The Auth method is not known";
        internal const string CertificateNotFound = "The certificate could not be accessed";
        internal const string InvalidMessageBusConfig = "Invalid Message bus config";
    }
}
