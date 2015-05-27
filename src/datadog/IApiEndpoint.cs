namespace Nohros.Metrics.Datadog
{
  public interface IApiEndpoint
  {
    bool PostSeries(string series);
  }
}
