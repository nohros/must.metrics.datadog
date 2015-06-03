using System.Configuration;
using Nohros.Extensions;

namespace Nohros.Metrics.Datadog.Config
{
  /// <summary>
  /// Defines a collection of <see cref="ApiEndpointConfig"/>.
  /// </summary>
  public class ApiEndpointConfigCollection : ConfigurationElementCollection
  {
    /// <sinheritdoc/>
    protected override ConfigurationElement CreateNewElement() {
      return new ApiEndpointConfig();
    }

    /// <sinheritdoc/>
    protected override object GetElementKey(ConfigurationElement element) {
      var config = (ApiEndpointConfig) element;
      return "{0}::{1}".Fmt(config.Uri, config.ApiKey);
    }
  }
}
