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
using System.Collections;
using System.Windows.Threading;
using System.Windows;

namespace SciGit_Client
{
  class SGRestClient
  {
    public const string serverHost = "scigit.sherk.me";
    private static string username = "";
    private static string authToken = "";
    private static int expiryTime = 0;

    public delegate void LoginResponseCallback(bool success);

    public static void Login(string username, string password, LoginResponseCallback callback, Dispatcher disp) {
      string uri = "https://" + serverHost + "/api/auth/login";
      uri += "?username=" + username;
      uri += "&password=" + password;
      WebRequest request = WebRequest.Create(uri);
      request.Method = "POST";
      request.Credentials = CredentialCache.DefaultCredentials;
      request.Timeout = 3000;

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
      //allows for validation of SSL certificates
      ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(
          (sender, cert, chain, errors) => true // allow unverified certificate, just for testing purposes
      );

      try {
        WebResponse response = request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        SGRestClient.username = username;
        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        XmlDocument doc = new XmlDocument();
        doc.Load(reader);
        XmlNode authTokenNode = doc.SelectSingleNode("//auth_token");
        XmlNode expiryTsNode = doc.SelectSingleNode("//expiry_ts");

        authToken = authTokenNode.InnerText;
        expiryTime = Int32.Parse(expiryTsNode.InnerText);
        disp.Invoke(callback, true);
      } catch (WebException e) {
        Debug.WriteLine(e);
        disp.Invoke(callback, false);
      }
    }

    public static List<Project> GetProjects() {
      if (username == "") return null;

      string uri = "http://" + serverHost + "/api/projects";
      uri += "?username=" + username;
      uri += "&auth_token=" + authToken;
      WebRequest request = WebRequest.Create(uri);
      request.Timeout = 3000;

      try {
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        XmlDocument doc = new XmlDocument();
        doc.Load(reader);

        XmlNodeList projectNodes = doc.SelectNodes("//item");
        List<Project> projects = new List<Project>();
        foreach (XmlNode xmlNode in projectNodes) {
          Project p = new Project();
          p.Id = Int32.Parse(xmlNode.SelectSingleNode("id").InnerText);
          p.OwnerId = Int32.Parse(xmlNode.SelectSingleNode("owner_id").InnerText);
          p.Name = xmlNode.SelectSingleNode("name").InnerText;
          p.CreatedTime = Int32.Parse(xmlNode.SelectSingleNode("created_ts").InnerText);
          projects.Add(p);
        }
        return projects;
      } catch (WebException e) {
        Debug.Write(e);
        return null;
      }
    }

    public static bool UploadPublicKey(string key) {
      string uri = "http://" + serverHost + "/api/users/public_keys";
      uri += "?username=" + username;
      uri += "&auth_token=" + authToken;
      WebRequest request = WebRequest.Create(uri);
      request.Method = "PUT";
      request.Timeout = 3000;
      Stream reqStream = request.GetRequestStream();
      byte[] postData = Encoding.UTF8.GetBytes("name=" + Environment.MachineName + "&public_key=" + Uri.EscapeDataString(key));
      reqStream.Write(postData, 0, postData.Length);

      try {
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();
        return true;
      } catch (WebException e) {
        HttpWebResponse response = (HttpWebResponse)e.Response;
        if (response.StatusCode == HttpStatusCode.Conflict) {
          return true; // just a duplicate key
        }
      }

      return false;
    }
  }
}
