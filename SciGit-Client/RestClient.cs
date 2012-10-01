﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Threading;
using System.Xml;

namespace SciGit_Client
{
  class RestClient
  {
    #region Delegates

    public delegate void LoginResponseCallback(bool success);

    #endregion

    public const string serverHost = "stage.scigit.sherk.me";
    public static int timeout = 20000;
    public static string username = "";
    private static string authToken = "";
    private static int expiryTime;

    public static void Login(string username, string password, LoginResponseCallback callback, Dispatcher disp) {
      string uri = "https://" + serverHost + "/api/auth/login";
      WebRequest request = WebRequest.Create(uri);
      request.Method = "POST";
      request.Credentials = CredentialCache.DefaultCredentials;
      request.Timeout = timeout;
      request.ContentType = "application/x-www-form-urlencoded";
      WriteData(request, new Dictionary<String, String> {
        { "username", username },
        { "password", password }
      });

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
      //allows for validation of SSL certificates
      ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;

      try {
        WebResponse response = request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        RestClient.username = username;
        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        var doc = new XmlDocument();
        doc.Load(reader);
        XmlNode authTokenNode = doc.SelectSingleNode("//auth_token");
        XmlNode expiryTsNode = doc.SelectSingleNode("//expiry_ts");

        authToken = authTokenNode.InnerText;
        expiryTime = Int32.Parse(expiryTsNode.InnerText);
        disp.Invoke(callback, true);
      } catch (Exception e) {
        Debug.WriteLine(e);
        disp.Invoke(callback, false);
      }
    }

    public static List<Project> GetProjects() {
      if (username == "") return null;

      string uri = "http://" + serverHost + "/api/projects";
      uri += "?" + GetQueryString(new Dictionary<String, String> {
        { "username", username },
        { "auth_token", authToken }
      });
      WebRequest request = WebRequest.Create(uri);
      request.Timeout = timeout;

      try {
        var response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        var doc = new XmlDocument();
        doc.Load(reader);

        XmlNodeList projectNodes = doc.SelectNodes("//item");
        var projects = new List<Project>();
        foreach (XmlNode xmlNode in projectNodes) {
          var p = new Project();
          p.Id = Int32.Parse(xmlNode.SelectSingleNode("id").InnerText);
          p.OwnerId = Int32.Parse(xmlNode.SelectSingleNode("owner_id").InnerText);
          p.Name = xmlNode.SelectSingleNode("name").InnerText;
          p.CreatedTime = Int32.Parse(xmlNode.SelectSingleNode("created_ts").InnerText);
          p.LastCommitHash = xmlNode.SelectSingleNode("last_commit_hash").InnerText;
          projects.Add(p);
        }
        return projects;
      } catch (Exception e) {
        Debug.WriteLine(e);
        return null;
      }
    }

    public static bool UploadPublicKey(string key) {
      string uri = "http://" + serverHost + "/api/users/public_keys";
      WebRequest request = WebRequest.Create(uri);
      request.Method = "PUT";
      request.Timeout = timeout;
      WriteData(request, new Dictionary<String, String> {
        { "username", username },
        { "auth_token", authToken },
        { "name", Environment.MachineName },
        { "public_key", key }
      });

      try {
        var response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();
        return true;
      }
      catch (WebException e) {
        var response = (HttpWebResponse)e.Response;
        if (response.StatusCode == HttpStatusCode.Conflict) {
          return true; // just a duplicate key
        }
        Debug.WriteLine(e);
      }
      catch (Exception e) {
        Debug.WriteLine(e);
      }

      return false;
    }

    private static string GetQueryString(Dictionary<String, String> data) {
      return String.Join("&", data.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
    }

    private static void WriteData(WebRequest request, Dictionary<String, String> data) {
      Stream reqStream = request.GetRequestStream();
      byte[] encodedData = Encoding.UTF8.GetBytes(GetQueryString(data));
      reqStream.Write(encodedData, 0, encodedData.Length);
    }
  }
}
