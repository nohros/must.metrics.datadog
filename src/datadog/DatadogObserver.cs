using System;
using Nohros.Data.Json;
using Nohros.Extensions.Time;
using Nohros.Metrics.Reporting;

namespace Nohros.Metrics.Datadog
{
  internal class DatadogObserver : IMeasureObserver
  {
    readonly IApiEndpoint endpoint_;
    readonly string host_;

    public DatadogObserver(IApiEndpoint endpoint, string host) {
      if (host == null) {
        throw new ArgumentNullException("host");
      }
      endpoint_ = endpoint;
      host_ = host;
    }

    public void Observe(Measure measure, DateTime timestamp) {
      MetricConfig config = measure.MetricConfig;
      Tags tags = config.Tags;

      string json =
        new JsonStringBuilder()
          .WriteBeginObject()
            .WriteMemberName("series")
            .WriteBeginArray()
              .WriteBeginObject()
                .WriteMember("metric", config.Name)
                .WriteMemberName("points")
                .WriteBeginArray()
                  .WriteBeginArray()
                    .WriteNumber(timestamp.ToUnixEpoch())
                    .WriteNumber(measure.Value)
                  .WriteEndArray()
                .WriteEndArray()
                .WriteMember("type", "gauge")
                .WriteMember("host", host_)
                .WriteMemberName("tags")
                .WriteBeginArray()
                  .ForEach(tags, (tag, builder) =>
                    builder
                      .WriteString(tag.Name + ":" + tag.Value))
                .WriteEndArray()
              .WriteEndObject()
            .WriteEndArray()
          .WriteEndObject()
          .ToString();
      endpoint_.PostSeries(json);
    }
  }
}
