using System.Configuration;

namespace Nohros.Metrics.Datadog.Config
{
  public class ApiKeysConfig : ConfigurationSection
  {
    [ConfigurationProperty("", IsRequired = false, IsKey = false,
      IsDefaultCollection = true)]
    public ApiKeyConfigColletion ApiKeys {
      get { return this[""] as ApiKeyConfigColletion; }
      set { this[""] = value; }
    }
  }
}
