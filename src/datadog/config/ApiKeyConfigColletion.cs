using System.Configuration;

namespace Nohros.Metrics.Datadog.Config
{
  public class ApiKeyConfigColletion : ConfigurationElementCollection
  {
    protected override ConfigurationElement CreateNewElement() {
      return new ApiKeysConfig();
    }

    protected override object GetElementKey(ConfigurationElement element) {
      var config = (ApiKeyConfig) element;
      return config.ApiKey;
    }
  }
}
