using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace SciGit_Client
{
    class SGRestClient
    {
        private const string ServerHost = "scigit.sherk.me";
        private static string Username = "";
        private static string AuthToken = "";
        private static int ExpiryTime = 0;

        public static bool Login(string username, string password)
        {
            string uri = "https://" + ServerHost + "/api/auth/login";
            uri += "?username=" + username;
            uri += "&password=" + password;
            WebRequest request = WebRequest.Create(uri);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Timeout = 3000;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            //allows for validation of SSL certificates
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();

                Username = username;
                XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                XmlNode authToken = doc.SelectSingleNode("//auth_token");
                XmlNode expiryTs = doc.SelectSingleNode("//expiry_ts");

                AuthToken = authToken.InnerText;
                ExpiryTime = Int32.Parse(expiryTs.InnerText);

                return true;
            }
            catch (WebException e)
            {
                Debug.Write(e);
                return false;
            }
        }

        //for testing purpose only, accept any dodgy certificate... 
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
