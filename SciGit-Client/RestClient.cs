using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Threading;
using System.Xml;
using SciGit_Client.Properties;

namespace SciGit_Client
{
  class RestClient
  {
    public enum Error
    {
      NoError,
      Forbidden,
      InvalidRequest,
      ConnectionError
    }

    public static readonly string ServerHost = App.Hostname;
    public static int Timeout = 20000;
    public static string Username = "";
    private static string AuthToken = "";
    private static int ExpiryTime;

    public static Tuple<bool, Error> Login(string username, string password) {
      string uri = "https://" + ServerHost + "/api/auth/login";
      Error error;

      try {
        WebRequest request = WebRequest.Create(uri);

        request.Method = "POST";
        request.Credentials = CredentialCache.DefaultCredentials;
        request.Timeout = Timeout;
        request.ContentType = "application/x-www-form-urlencoded";
        WriteData(request, new Dictionary<String, String> {
          { "username", username },
          { "password", password }
        });

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
        //allows for validation of SSL certificates
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;

        WebResponse response = request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        Username = username;
        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        var doc = new XmlDocument();
        doc.Load(reader);
        XmlNode authTokenNode = doc.SelectSingleNode("//auth_token");
        XmlNode expiryTsNode = doc.SelectSingleNode("//expiry_ts");

        AuthToken = authTokenNode.InnerText;
        ExpiryTime = Int32.Parse(expiryTsNode.InnerText);
        return Tuple.Create(true, Error.NoError);
      } catch (WebException e) {
        var response = (HttpWebResponse)e.Response;
        if (response == null) {
          error = Error.ConnectionError;
        } else if (response.StatusCode == HttpStatusCode.Forbidden) {
          error = Error.Forbidden;
        } else {
          error = Error.InvalidRequest;
        }
      } catch (Exception e) {
        error = Error.ConnectionError;
        Logger.LogException(e);
      }

      return Tuple.Create(false, error);
    }

    public static Tuple<List<Project>, Error> GetProjects() {
      if (Username == "") return null;

      Error error;
      string uri = "http://" + ServerHost + "/api/projects";      
      try {
        WebRequest request = WebRequest.Create(uri + "?" + GetQueryString(new Dictionary<String, String> {
          { "username", Username },
          { "auth_token", AuthToken }
        }));
        request.Timeout = Timeout;

        var response = (HttpWebResponse) request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        var doc = new XmlDocument();
        doc.Load(reader);

        XmlNodeList projectNodes = doc.SelectNodes("//item");
        var projects = new List<Project>();
        foreach (XmlNode xmlNode in projectNodes) {
          var p = new Project {
            Id = Int32.Parse(xmlNode.SelectSingleNode("id").InnerText),
            Name = xmlNode.SelectSingleNode("name").InnerText,
            CreatedTime = Int32.Parse(xmlNode.SelectSingleNode("created_ts").InnerText),
            LastCommitHash = xmlNode.SelectSingleNode("last_commit_hash").InnerText,
            CanWrite = Boolean.Parse(xmlNode.SelectSingleNode("can_write").InnerText)
          };
          projects.Add(p);
        }
        return Tuple.Create(projects, Error.NoError);
      } catch (WebException e) {
        var response = (HttpWebResponse)e.Response;
        if (response == null) {
          error = Error.ConnectionError;
        } else if (response.StatusCode == HttpStatusCode.Forbidden) {
          error = Error.Forbidden;
        } else {
          error = Error.InvalidRequest;
        }
      } catch (Exception e) {
        error = Error.ConnectionError;
        Logger.LogException(e);
      }

      return Tuple.Create<List<Project>, Error>(null, error);
    }

    public static Tuple<string, Error> GetLatestClientVersion() {
      Error error;
      string uri = "http://" + ServerHost + "/api/client_version";
      try {
        WebRequest request = WebRequest.Create(uri);
        request.Timeout = Timeout;

        var response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();

        XmlReader reader = JsonReaderWriterFactory.CreateJsonReader(dataStream, new XmlDictionaryReaderQuotas());
        var doc = new XmlDocument();
        doc.Load(reader);

        string version = doc.SelectSingleNode("//version").InnerText;
        return Tuple.Create(version, Error.NoError);
      } catch (WebException e) {
        var response = (HttpWebResponse)e.Response;
        if (response == null) {
          error = Error.ConnectionError;
        } else if (response.StatusCode == HttpStatusCode.Forbidden) {
          error = Error.Forbidden;
        } else {
          error = Error.InvalidRequest;
        }
      } catch (Exception e) {
        error = Error.ConnectionError;
        Logger.LogException(e);
      }

      return Tuple.Create<string, Error>(null, error);
    }

    public static bool? UploadPublicKey(string key) {
      string uri = "http://" + ServerHost + "/api/users/public_keys";

      try {
        WebRequest request = WebRequest.Create(uri);
        request.Method = "PUT";
        request.Timeout = Timeout;
        WriteData(request, new Dictionary<String, String> {
          { "username", Username },
          { "auth_token", AuthToken },
          { "name", Environment.MachineName },
          { "public_key", key }
        });
        var response = (HttpWebResponse)request.GetResponse();
        Stream dataStream = response.GetResponseStream();
        return true;
      } catch (WebException e) {
        var response = (HttpWebResponse)e.Response;
        if (response == null) {
          return false;
        } else if (response.StatusCode == HttpStatusCode.Conflict) {
          return true; // just a duplicate key
        } else if (response.StatusCode == HttpStatusCode.BadRequest) {
          return false;
        }
      } catch (Exception e) {
        Logger.LogException(e);
      }

      return null;
    }

    private static string GetQueryString(Dictionary<String, String> data) {
      return String.Join("&", data.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)).ToArray());
    }

    private static void WriteData(WebRequest request, Dictionary<String, String> data) {
      Stream reqStream = request.GetRequestStream();
      byte[] encodedData = Encoding.UTF8.GetBytes(GetQueryString(data));
      reqStream.Write(encodedData, 0, encodedData.Length);
      reqStream.Close();
    }
  }
}
