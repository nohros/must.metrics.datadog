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

    /// <summary>
    /// Specifies the maximum concurrency level for event sources.
    /// </summary>
    [ConfigurationProperty("Uri", IsRequired = false,
      DefaultValue = "https://app.datadoghq.com/api/v1")]
    public string Uri {
      get { return (string) this["Uri"]; }
      set { this["Uri"] = value; }
    }
  }
}
