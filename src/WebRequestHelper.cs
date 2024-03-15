using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    internal static class WebRequestHelper
    {
        #region Fields
        private const string EnvironmentWildcard = "{environment}";

        private static readonly string TerminalApiCnRegex = "[a-zA-Z0-9]{4,}-[0-9]{9}\\." + EnvironmentWildcard + "\\.terminal\\.adyen\\.com";
        private static readonly string TerminalApiLegacy = "legacy-terminal-certificate." + EnvironmentWildcard + ".terminal.adyen.com";

        private static EnvironmentType _environment;
        #endregion

        public static string Request(string endpoint, string json, EnvironmentType environment, string apiKey)
        {
            _environment = environment;
            using (var handler = GetHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback = ServerCertificateValidationCallback;
                using (var client = new HttpClient(handler))
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.Add("Accept-Charset", "UTF-8");
                    content.Headers.Add("Cache-Control", "no-cache");
                    content.Headers.Add("x-api-key", apiKey);
                    using (HttpResponseMessage response = client.PostAsync(endpoint, content).GetAwaiter().GetResult())
                    {
                        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
            }

            HttpClientHandler GetHandler() => new()
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };
        }

        public static string ReadRequestInputStream()
        {
            using (var receiveStream = Context.Current.Request.InputStream)
            {
                using (var readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    return readStream.ReadToEnd();
                }
            }
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.None:
                    return true;
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    return ValidateCertificate(certificate.Subject, _environment);
                default:
                    return false;
            }
        }

        private static bool ValidateCertificate(string certificateSubject, EnvironmentType environment)
        {
            var environmentName = environment.ToString().ToLower();
            var regexPatternTerminalSpecificCert = TerminalApiCnRegex.Replace(EnvironmentWildcard, environmentName);
            var regexPatternLegacyCert = TerminalApiLegacy.Replace(EnvironmentWildcard, environmentName);
            var subject = certificateSubject.Split(',')
                     .Select(x => x.Split('='))
                     .ToDictionary(x => x[0].Trim(' '), x => x[1]);
            if (subject.ContainsKey("CN"))
            {
                var commonNameValue = subject["CN"];
                if (Regex.Match(commonNameValue, regexPatternTerminalSpecificCert).Success || string.Equals(commonNameValue, regexPatternLegacyCert))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
