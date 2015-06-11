using System;
using System.IO;
using System.Net;
using System.Text;
using Nohros.Extensions;

namespace Nohros.Metrics.Datadog
{
  internal class ApiEndpoint : IApiEndpoint
  {
    const string kClassName = "Nohros.Metrics.Datadog.ApiEndpoint";

    const string kRequestPath = "series?api_key={0}";

    readonly CookieContainer cookies_;
    readonly Uri request_uri_;
    readonly DatadogLogger logger_;
    readonly IWebProxy proxy_;

    public ApiEndpoint(string endpoint_uri, string api_key)
      : this(new Uri(endpoint_uri), api_key, string.Empty) {
    }

    public ApiEndpoint(string endpoint_uri, string api_key, string proxy)
      : this(new Uri(endpoint_uri), api_key, proxy) {
    }

    public ApiEndpoint(Uri base_uri, string api_key)
      : this(base_uri, api_key, string.Empty) {
    }

    public ApiEndpoint(Uri base_uri, string api_key, string proxy) {
      if (base_uri == null || api_key == null) {
        throw new ArgumentNullException(base_uri == null
          ? "base_uri"
          : "api_key");
      }

      request_uri_ = new Uri(base_uri, kRequestPath.Fmt(api_key));
      cookies_ = new CookieContainer();

      logger_ = DatadogLogger.ForCurrentProcess;

      ServicePointManager.Expect100Continue = false;

      proxy_ = GetProxy(proxy == null ? string.Empty : proxy.Trim());
    }

    IWebProxy GetProxy(string proxy) {
      IWebProxy default_proxy = WebRequest.DefaultWebProxy;
      if (proxy == string.Empty) {
        return default_proxy;
      }

      string[] uri_parts = proxy.Split('@');
      if (uri_parts.Length == 1) {
        return new WebProxy(uri_parts[0]);
      }

      if (uri_parts.Length != 2) {
        return default_proxy;
      }

      var web_proxy = new WebProxy(uri_parts[1]);
      string[] login_parts = uri_parts[0].Split(':');
      if (login_parts.Length != 2) {
        return default_proxy;
      }

      web_proxy.Credentials =
        new NetworkCredential(login_parts[0], login_parts[1]);
      return web_proxy;
    }

    public bool PostSeries(string series) {
      return HasSucceed(Post(series));
    }

    bool HasSucceed(HttpWebResponse response) {
      bool accepted = response.StatusCode == HttpStatusCode.Accepted;
      if (!accepted) {
        logger_.Warn(R.Endpoint_SeriesPostFail.Fmt(response.StatusDescription));
      }
      return accepted;
    }

    HttpWebResponse Post(string json) {
      var request = CreateRequest();
      request.KeepAlive = true;
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

    HttpWebRequest CreateRequest() {
      var request = (HttpWebRequest) WebRequest.Create(request_uri_);
      request.Proxy = proxy_;
      return request;
    }
  }
}
