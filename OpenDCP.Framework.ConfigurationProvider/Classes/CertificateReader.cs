using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenDCP.Framework.ConfigurationProvider.Classes
{
    public class CertificateReader
    {

        public X509Certificate2Collection FindCertificateByName(string certName)
        {
            X509Store store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var cers = new X509Certificate2Collection();
            foreach (var item in store.Certificates)
            {
                var subjectAlternativeNames = GetSubjectAlternativeNames(item);

                if (subjectAlternativeNames.Contains(certName))
                {

                    cers.Add(item);
                    break;
                }
            }
            store.Close();

            return cers;
        }

        public List<string> GetSubjectAlternativeNames(X509Certificate2 cert)
        {
            var result = new List<string>();

            var subjectAlternativeNames = cert.Extensions.Cast<X509Extension>()
                                                .Where(n => n.Oid.Value == "2.5.29.17") //Subject Alternative Name
                                                .Select(n => new AsnEncodedData(n.Oid, n.RawData))
                                                .Select(n => n.Format(true))
                                                .FirstOrDefault();

            if (string.IsNullOrEmpty(subjectAlternativeNames))
            {
                return result;
            }

            // Example outputs:
            // Windows: "DNS Name=example.com, DNS Name=www.example.com"
            // Windows NL: "DNS-naam=example.com\r\nDNS-naam=www.example.com\r\n"
            // Linux: "DNS:example.com, DNS:www.example.com"

            var delimiters = new char[] { '=', ':' };
            var notfound = -1;
            var pairs = subjectAlternativeNames.Split(new[] { ",", "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                int position = pair.IndexOfAny(delimiters);
                if (position == notfound)
                    continue;

                var subjectAlternativeName = pair.Substring(position + 1);
                if (string.IsNullOrEmpty(subjectAlternativeName))
                    continue;

                result.Add(subjectAlternativeName);
            }
            return result;
        }
    }
}
