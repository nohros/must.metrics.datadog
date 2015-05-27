using System;
using System.IO;
using System.Net;
using System.Text;
using Nohros.Extensions;
using Nohros.Logging;

namespace Nohros.Metrics.Datadog
{
  internal class ApiEndpoint : IApiEndpoint
  {
    const string kClassName = "Nohros.Metrics.Datadog.ApiEndpoint";

    const string kRequestPath = "series?api_key={0}";

    readonly CookieContainer cookies_;
    readonly Uri request_uri_;

    public ApiEndpoint(string base_uri, string api_key)
      : this(new Uri(base_uri), api_key) {
    }

    public ApiEndpoint(Uri base_uri, string api_key) {
      request_uri_ = new Uri(base_uri, kRequestPath.Fmt(api_key));
      cookies_ = new CookieContainer();
    }

    public bool PostSeries(string series) {
      return HasSucceed(Post(series));
    }

    bool HasSucceed(HttpWebResponse response) {
      bool accepted = response.StatusCode == HttpStatusCode.Accepted;
      if (!accepted) {
        MustLogger.ForCurrentProcess.Warn(
          "Series failed to be posted with code:{0}".Fmt(
            response.StatusDescription));
      }
      return accepted;
    }

    HttpWebResponse Post(string json) {
      var request = (HttpWebRequest) WebRequest.Create(request_uri_);
      request.KeepAlive = false;
      request.CookieContainer = cookies_;
      request.Accept = "application/json";
      request.ContentType = "application/json";

      byte[] data = Encoding.UTF8.GetBytes(json);

      request.Method = "POST";
      request.ContentLength = data.Length;

      using (Stream stream = request.GetRequestStream()) {
        stream.Write(data, 0, data.Length);
        stream.Close();
      }
      return (HttpWebResponse) request.GetResponse();
    }
  }
}
