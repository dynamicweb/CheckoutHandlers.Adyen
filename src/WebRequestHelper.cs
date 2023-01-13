using System;
using System.IO;
using System.Linq;
using System.Net;
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
            var httpWebRequest = CreateWebRequest(endpoint, apiKey);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            try
            {
                using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw e;
                }

                var response = (HttpWebResponse)e.Response;
                var responseText = string.Empty; 
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }
                throw new WebException(responseText, e);
            }
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

        private static HttpWebRequest CreateWebRequest(string endpoint, string apiKey)
        {
            //Add default headers
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add("Accept-Charset", "UTF-8");
            httpWebRequest.Headers.Add("Cache-Control", "no-cache");
            httpWebRequest.Headers.Add("x-api-key", apiKey);
            httpWebRequest.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            return httpWebRequest;
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
