using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Nohros.Metrics.Datadog.Config
{
  public class ApiKeyConfig : ConfigurationElement
  {
    [ConfigurationProperty("ApiKey", IsRequired = true)]
    public string ApiKey {
      get {
        var key = (string) this["ApiKey"];
        return Encrypted ? Decrypt(key) : key;
      }
      set {
        this["ApiKey"] = value;
      }
    }

    [ConfigurationProperty("Encrypted", IsRequired = false, DefaultValue = false)]
    public bool Encrypted {
      get { return (bool) this["Encrypted"]; }
      set { this["Encrypted"] = value; }
    }

    string Decrypt(string encrypted) {
      byte[] data = Convert.FromBase64String(encrypted);
      byte[] decrypted =
        ProtectedData
          .Unprotect(data, new byte[0], DataProtectionScope.LocalMachine);
      return Encoding.Unicode.GetString(decrypted);
    }
  }
}
