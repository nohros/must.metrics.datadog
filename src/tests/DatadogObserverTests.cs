using System;
using System.Threading;
using NUnit.Framework;
using Nohros.Extensions;
using Nohros.Extensions.Time;
using Nohros.Metrics;
using Nohros.Metrics.Datadog;

namespace Must.Metrics.Datadog.Tests
{
  public class DatadogObserverTests
  {
    class ApiEndpointMock : IApiEndpoint
    {
      public bool PostSeries(string series) {
        PostedSeries = series;
        return true;
      }

      public string PostedSeries { get; set; }
    }

    [Test]
    public void should_serialize_measure_into_datadog_format() {
      var tags =
        new Tags.Builder()
          .WithTag("tag1", "tagValue1")
          .Build();
      var config = new MetricConfig("myMetric", tags);
      var measure = new Measure(config, 1000);

      var date = DateTime.Now;
      var api = new ApiEndpointMock();
      var observer = new DatadogObserver(api, "ACAOAFPAPPBACK",
        TimeSpan.FromMilliseconds(50));
      observer.Observe(measure, date);
      observer.Observe(measure, date);

      string json =
        "{{\"series\":[{{\"metric\":\"myMetric\",\"points\":[[{0},{1}]],\"type\":\"gauge\",\"host\":\"{2}\",\"tags\":[\"{3}\"]}}]}}"
          .Fmt(date.ToUnixEpoch(), measure.Value, "ACAOAFPAPPBACK",
            "tag1:tagValue1");

      string json2 =
        "{{\"series\":[{{\"metric\":\"myMetric\",\"points\":[[{0},{1}]],\"type\":\"gauge\",\"host\":\"{2}\",\"tags\":[\"{3}\"]}},{{\"metric\":\"myMetric\",\"points\":[[{0},{1}]],\"type\":\"gauge\",\"host\":\"{2}\",\"tags\":[\"{3}\"]}}]}}"
          .Fmt(date.ToUnixEpoch(), measure.Value, "ACAOAFPAPPBACK",
            "tag1:tagValue1");

      Thread.Sleep(TimeSpan.FromMilliseconds(100));

      Assert.That(api.PostedSeries, Is.EqualTo(json2));
    }
  }
}
