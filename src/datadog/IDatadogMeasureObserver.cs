using Nohros.Metrics.Reporting;

namespace Nohros.Metrics.Datadog
{
  /// <summary>
  /// Defines a <see cref="IMeasureObserver"/> that send measures
  /// to a datadog endpoint.
  /// </summary>
  /// <remarks>
  /// </remarks>
  public interface IDatadogMeasureObserver : IMeasureObserver
  {
    /// <summary>
    /// Start sending metrics to the datadog's endpoint.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop sending metrics to the datadog's endpoint.
    /// </summary>
    void Stop();
  }
}
