using System;
using System.Configuration;
using Nohros.Configuration;

namespace Nohros.Metrics.Datadog.Config
{
  /// <summary>
  /// Defines the configuration elements for the datadog.
  /// </summary>
  public class ApiEndpointConfig : EncryptedConfigurationElement
  {
    /// <summary>
    /// The api key to be uses when sending data do datadog's endpoint
    /// </summary>
    [ConfigurationProperty("ApiKey", IsRequired = true)]
    public string ApiKey {
      get {
        var key = (string) this["ApiKey"];
        return Encrypted ? Decrypt(key) : key;
      }
      set { this["ApiKey"] = value; }
    }

    /// <summary>
    /// Specifies the name of the host.
    /// </summary>
    /// <remarks>
    /// If the host's name is not specified the value of
    /// <see cref="Environment.MachineName"/> will be used as the host.
    /// </remarks>
    [ConfigurationProperty("Host", IsRequired = false, DefaultValue = "")]
    public string Host {
      get { return (string) this["Host"]; }
      set { this["Host"] = value; }
    }

    /// <summary>
    /// Specifies the application's name.
    /// </summary>
    /// <remarks>
    /// The application name is appended to the name of the metrics as a
    /// prefix when it is reported to the datadog's endpoint.
    /// </remarks>
    [ConfigurationProperty("AppName", IsRequired = false, DefaultValue = "")]
    public string AppName {
      get { return (string) this["AppName"]; }
      set { this["AppName"] = value; }
    }

    /// <summary>
    /// Specifies the uri of the datadog endpoint.
    /// </summary>
    [ConfigurationProperty("Uri", IsRequired = false,
      DefaultValue = "https://app.datadoghq.com/api/v1")]
    public string Uri {
      get { return (string) this["Uri"]; }
      set { this["Uri"] = value; }
    }
  }
}
