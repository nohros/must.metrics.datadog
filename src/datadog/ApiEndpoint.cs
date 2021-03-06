﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Nohros.Extensions;

namespace Nohros.Metrics.Datadog
{
  /// <summary>
  /// A implementation of the <see cref="IApiEndpoint"/> class.
  /// </summary>
  internal class ApiEndpoint : IApiEndpoint
  {
    /// <summary>
    /// Series retry metadata.
    /// </summary>
    class Retry
    {
      public Retry(string series) {
        Series = series;
        RetryAttempt = 0;
      }

      public string Series { get; set; }
      public int RetryAttempt { get; set; }
    }

    const string kClassName = "Nohros.Metrics.Datadog.ApiEndpoint";

    const int kMaxRetryAttempts = 5;

    const string kRequestPath = "series?api_key={0}";

    readonly CookieContainer cookies_;
    readonly Uri request_uri_;
    readonly DatadogLogger logger_;
    readonly IWebProxy proxy_;
    readonly Queue<Retry> retries_;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEndpoint"/> by
    /// using the given endpoint uri and api key.
    /// </summary>
    /// <param name="endpoint_uri">
    /// The address to the datadog endpoint.
    /// </param>
    /// <param name="api_key">
    /// The api key for the datadog application.
    /// </param>
    public ApiEndpoint(string endpoint_uri, string api_key)
      : this(new Uri(endpoint_uri), api_key, string.Empty) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEndpoint"/> by
    /// using the given endpoint uri and api key.
    /// </summary>
    /// <param name="endpoint_uri">
    /// The address to the datadog endpoint.
    /// </param>
    /// <param name="api_key">
    /// The api key for the datadog application.
    /// </param>
    /// <param name="proxy">
    /// A string containing the proxy to be used to post the series to
    /// datadog servers. The proxy should be specified in the format:
    /// "http[s]://[username]:[password]@proxy.com"
    /// </param>
    public ApiEndpoint(string endpoint_uri, string api_key, string proxy)
      : this(new Uri(endpoint_uri), api_key, proxy) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEndpoint"/> by
    /// using the given endpoint uri and api key.
    /// </summary>
    /// <param name="base_uri">
    /// The address to the datadog endpoint.
    /// </param>
    /// <param name="api_key">
    /// The api key for the datadog application.
    /// </param>
    public ApiEndpoint(Uri base_uri, string api_key)
      : this(base_uri, api_key, string.Empty) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEndpoint"/> by
    /// using the given endpoint uri and api key.
    /// </summary>
    /// <param name="base_uri">
    /// The address to the datadog endpoint.
    /// </param>
    /// <param name="api_key">
    /// The api key for the datadog application.
    /// </param>
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

      retries_ = new Queue<Retry>();
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

    /// <inheritdoc/>
    public bool PostSeries(string series) {
      bool posted = Post(series);
      if (posted) {
        // The series was sucessfully posted, lets try to post the series
        // that has been failed in the past.
        while (retries_.Count > 0) {
          Retry retry = retries_.Peek();
          if (retry.RetryAttempt <= kMaxRetryAttempts) {
            posted = Post(retry.Series);
            if (!posted) {
              retry.RetryAttempt++;
              break;
            }
          } else {
            logger_.Warn(R.Endpoint_GivingUpRetry.Fmt(series));
          }
          retries_.Dequeue();
        }
      }
      return posted;
    }

    bool Post(string series) {
      try {
        using (var response = HttpPost(series)) {
          return HasSucceed(response);
        }
      } catch (WebException ex) {
        switch (ex.Status) {
          case WebExceptionStatus.KeepAliveFailure:
          case WebExceptionStatus.ConnectFailure:
          case WebExceptionStatus.ConnectionClosed:
          case WebExceptionStatus.Timeout:
            logger_.Error(R.Endpoint_WebException_PostFailRetry, ex);
            return false;
        }
        throw;
      } catch (IOException io) {
        logger_.Error(R.Endpoint_WebException_PostFailRetry, io);
        return false;
      }
    }

    bool HasSucceed(HttpWebResponse response) {
      bool accepted = response.StatusCode == HttpStatusCode.Accepted;
      if (!accepted) {
        logger_.Warn(R.Endpoint_SeriesPostFail.Fmt(response.StatusDescription));
      }
      return accepted;
    }

    HttpWebResponse HttpPost(string json) {
      var request = CreateRequest();
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
