using System;
using System.Collections.Generic;
using System.Net;

namespace SciGit_Client
{
  class RestClient
  {
    public enum ErrorType
    {
      NoError,
      Forbidden,
      InvalidRequest,
      ConnectionError
    }

    public class Response<T>
    {
      public T Data { get; set; }
      public ErrorType Error { get; set; }
      public Response(HttpStatusCode code) {
        if (code == HttpStatusCode.Forbidden) {
          Error = ErrorType.Forbidden;
        } else if (code == HttpStatusCode.BadRequest) {
          Error = ErrorType.InvalidRequest;
        } else if (code != HttpStatusCode.OK) {
          Error = ErrorType.ConnectionError;
        } else {
          Error = ErrorType.NoError;
        }
      }
      public Response(ErrorType error) {
        // Leave Data at default value
        Error = error;
      }
      public Response(T data) {
        Data = data;
        Error = ErrorType.NoError; 
      }
    }

    public static readonly string ServerHost = App.Hostname;
    public static int Timeout = 10000;
    public static string Username = "";
    private static string AuthToken = "";
    private static int ExpiryTime;

    public static Response<bool> Login(string username, string password) {
      try {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
#if STAGE
        // allows for validation of invalid SSL certificates
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
#endif

        var client = new RestSharp.RestClient("https://" + ServerHost) {Timeout = Timeout};
        var request = new RestSharp.RestRequest("api/auth/login", RestSharp.Method.POST);
        request.AddParameter("username", username);
        request.AddParameter("password", password);
        var response = client.Execute<Dictionary<string, string>>(request);
        if (response.StatusCode != HttpStatusCode.OK) {
          return new Response<bool>(response.StatusCode);
        }
        Username = username;
        AuthToken = response.Data["auth_token"];
        ExpiryTime = int.Parse(response.Data["expiry_ts"]);
        return new Response<bool>(true);
      } catch (Exception e) {
        Logger.LogException(e);
      }

      return new Response<bool>(ErrorType.ConnectionError);
    }

    public static Response<List<Project>> GetProjects() {
      try {
        var client = new RestSharp.RestClient("http://" + ServerHost) {Timeout = Timeout};
        var request = new RestSharp.RestRequest("api/projects");
        request.AddParameter("username", Username);
        request.AddParameter("auth_token", AuthToken);
        var response = client.Execute<List<Project>>(request);
        if (response.StatusCode != HttpStatusCode.OK) {
          return new Response<List<Project>>(response.StatusCode);
        }
        return new Response<List<Project>>(response.Data);
      } catch (Exception e) {
        Logger.LogException(e);
        return new Response<List<Project>>(ErrorType.ConnectionError);
      }
    }

    public static RestClient.Response<string> GetLatestClientVersion() {
      try {
        var client = new RestSharp.RestClient("http://" + ServerHost) { Timeout = Timeout };
        var request = new RestSharp.RestRequest("api/client_version");
        request.AddParameter("username", Username);
        request.AddParameter("auth_token", AuthToken);
        var response = client.Execute<Dictionary<string, string>>(request);
        if (response.StatusCode != HttpStatusCode.OK) {
          return new Response<string>(response.StatusCode);
        }
        return new Response<string>(response.Data["version"]);
      } catch (Exception e) {
        Logger.LogException(e);
        return new Response<string>(ErrorType.ConnectionError);
      }
    }

    public static bool? UploadPublicKey(string key) {
      try {
        var client = new RestSharp.RestClient("http://" + ServerHost) { Timeout = Timeout };
        var request = new RestSharp.RestRequest("api/users/public_keys", RestSharp.Method.PUT);
        request.AddParameter("username", Username);
        request.AddParameter("auth_token", AuthToken);
        request.AddParameter("name", Environment.MachineName);
        request.AddParameter("public_key", key);
        var response = client.Execute(request);
        if (response.StatusCode == HttpStatusCode.Conflict) {
          return true; // Key already exists, but that's fine
        } else if (response.StatusCode != HttpStatusCode.OK) {
          return false;
        }
        return true;
      } catch (Exception e) {
        Logger.LogException(e);
      }

      return null;
    }
  }
}
