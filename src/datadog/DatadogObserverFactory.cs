using System.Collections.Generic;
using Nohros.Metrics.Datadog.Config;
using Nohros.Metrics.Reporting;
using System.Linq;

namespace Nohros.Metrics.Datadog
{
  public class DatadogObserverFactory
  {
    public IEnumerable<IMeasureObserver> Create(string base_uri, string host,
      ApiKeysConfig section) {
      return section
        .ApiKeys
        .Cast<ApiKeyConfig>()
        .Select(config => Create(base_uri, host, config));
    }

    public IMeasureObserver Create(string base_uri, string host,
      ApiKeyConfig config) {
      return Create(base_uri, host, config.ApiKey);
    }

    public IMeasureObserver Create(string base_uri, string host, string api_key) {
      var endpoint = new ApiEndpoint(base_uri, api_key);
      return new DatadogObserver(endpoint, host);
    }
  }
}
