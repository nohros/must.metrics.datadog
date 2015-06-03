using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Nohros.Concurrent;
using Nohros.Data.Json;
using Nohros.Extensions.Time;
using System.Linq;
using Nohros.Resources;

namespace Nohros.Metrics.Datadog
{
  internal class DatadogObserver : IDatadogMeasureObserver
  {
    class Serie
    {
      public string Name { get; set; }
      public double Measure { get; set; }
      public string[] Tags { get; set; }
      public DateTime Timestamp { get; set; }
    }

    readonly IApiEndpoint endpoint_;
    readonly string host_;
    readonly string app_;
    readonly ConcurrentQueue<Serie> measures_;
    readonly NonReentrantSchedule scheduler_;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogObserver"/> by
    /// using the given <paramref name="endpoint"/> and <paramref name="host"/>.
    /// </summary>
    /// <param name="endpoint">
    /// The <see cref="ApiEndpoint"/> to be used to send the measures.
    /// </param>
    /// <param name="host">
    /// The name of the host that should be associated with the measures.
    /// </param>
    public DatadogObserver(IApiEndpoint endpoint, string host)
      : this(endpoint, host, TimeSpan.FromSeconds(30)) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogObserver"/> by
    /// using the given <paramref name="endpoint"/>, <paramref name="host"/>
    /// and <paramref name="ttl"/>.
    /// </summary>
    /// <param name="endpoint">
    /// The <see cref="ApiEndpoint"/> to be used to send the measures.
    /// </param>
    /// <param name="host">
    /// The name of the host that should be associated with the measures.
    /// </param>
    /// <param name="ttl">
    /// The maximum time that a mesure should be keep in cache, before send it
    /// to datadog's.
    /// </param>
    /// <remarks>
    /// The <paramref name="ttl"/> should be greater than or equals to
    /// <see cref="TimeSpan.Zero"/>. If <paramref name="ttl"/> is equals to
    /// <see cref="TimeSpan.Zero"/> the default <paramref name="ttl"/> of
    /// third seconds will be used.
    /// </remarks>
    public DatadogObserver(IApiEndpoint endpoint, string host, TimeSpan ttl)
      : this(endpoint, host, ttl, string.Empty) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogObserver"/> by
    /// using the given <paramref name="endpoint"/>, <paramref name="host"/>
    /// <paramref name="ttl"/> and <paramref name="app"/>.
    /// </summary>
    /// <param name="endpoint">
    /// The <see cref="ApiEndpoint"/> to be used to send the measures.
    /// </param>
    /// <param name="host">
    /// The name of the host that should be associated with the measures.
    /// </param>
    /// <param name="ttl">
    /// The maximum time that a mesure should be keep in cache, before send it
    /// to datadog's.
    /// </param>
    /// <param name="app">
    /// The application's name.
    /// </param>
    /// <remarks>
    /// The <paramref name="ttl"/> should be greater than or equals to
    /// <see cref="TimeSpan.Zero"/>. If <paramref name="ttl"/> is equals to
    /// <see cref="TimeSpan.Zero"/> the default <paramref name="ttl"/> of
    /// third seconds will be used.
    /// <para>
    /// The application's name will be added as a prefix to all measures
    /// before sending it to datadog's endpoint.
    /// </para>
    /// </remarks>
    public DatadogObserver(IApiEndpoint endpoint, string host, TimeSpan ttl,
      string app) {
      if (host == null) {
        throw new ArgumentNullException("host");
      }

      if (ttl < TimeSpan.Zero) {
        throw new ArgumentOutOfRangeException("ttl",
          StringResources.ArgumentOutOfRange_NeedNonNegNum);
      }

      endpoint_ = endpoint;
      host_ = host;
      app_ = app.Trim();

      // The app should be separated from the metric's name with a dot.
      if (app_ != "" && app_.EndsWith(".")) {
        app_ += ".";
      }

      measures_ = new ConcurrentQueue<Serie>();

      TimeSpan half_ttl =
        ttl == TimeSpan.Zero
          ? TimeSpan.FromSeconds(15)
          : TimeSpan.FromSeconds(ttl.TotalSeconds/2);
      scheduler_ = NonReentrantSchedule.Every(half_ttl);
      scheduler_.Run(Post);
    }

    public void Start() {
      scheduler_.Run(Post);
    }

    public void Stop() {
      scheduler_.Stop().WaitOne();
    }

    void Post() {
      Serie serie;
      var series = new List<Serie>(measures_.Count);
      while (measures_.TryDequeue(out serie)) {
        series.Add(serie);
      }

      if (series.Count > 0) {
        JsonStringBuilder json =
          new JsonStringBuilder()
            .WriteBeginObject()
            .WriteMemberName("series")
            .WriteBeginArray()
            .ForEach(series, WriteSerie)
            .WriteEndArray()
            .WriteEndObject();
        endpoint_.PostSeries(json.ToString());
      }
    }

    void WriteSerie(Serie serie, JsonStringBuilder json) {
      json
        .WriteBeginObject()
        .WriteMember("metric", app_ + serie.Name)
        .WriteMemberName("points")
        .WriteBeginArray()
        .WriteBeginArray()
        .WriteNumber(serie.Timestamp.ToUnixEpoch())
        .WriteNumber(serie.Measure)
        .WriteEndArray()
        .WriteEndArray()
        .WriteMember("type", "gauge")
        .WriteMember("host", host_)
        .WriteMemberName("tags")
        .WriteBeginArray()
        .ForEach(serie.Tags, (tag, builder) => builder.WriteString(tag))
        .WriteEndArray()
        .WriteEndObject();
    }

    public void Observe(Measure measure, DateTime timestamp) {
      var serie = new Serie {
        Measure = measure.Value,
        Name = measure.MetricConfig.Name,
        Timestamp = timestamp
      };

      Tag[] tags = measure.MetricConfig.Tags.ToArray();
      var plain_tags = new string[tags.Length];
      for (int i = 0; i < tags.Length; i++) {
        Tag tag = tags[i];
        plain_tags[i] = tag.Name + ":" + tag.Value;
      }
      serie.Tags = plain_tags;

      // If the limit is 1, publish the measure directly to avoid storing it
      // into the queue.
      measures_.Enqueue(serie);
    }
  }
}
