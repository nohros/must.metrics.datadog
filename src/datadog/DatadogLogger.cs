using System;
using Nohros.Logging;

namespace Nohros.Metrics.Datadog
{
  public class DatadogLogger : ForwardingLogger
  {
    static DatadogLogger() {
      ForCurrentProcess = new DatadogLogger(new NOPLogger());
    }

    public DatadogLogger(ILogger logger) : base(logger) {
    }

    public static DatadogLogger ForCurrentProcess { get; set; }
  }
}
