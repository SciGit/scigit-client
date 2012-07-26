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

namespace SciGit_Client
{
  class SGRestClient
  {
    private const string ServerHost = "scigit.sherk.me";
    private static string Username = "";
    private static string AuthToken = "";
    private static int ExpiryTime = 0;

    public static bool Login(string username, string password) {
      string uri = "https://" + ServerHost + "/api/auth/login";
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
      } catch (WebException e) {
        Debug.Write(e);
        return false;
      }
    }

    public static List<Project> GetProjects() {
      if (Username == "") return null;

      string uri = "http://" + ServerHost + "/api/projects";
      uri += "?username=" + Username;
      uri += "&auth_token=" + AuthToken;
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
  }
}
