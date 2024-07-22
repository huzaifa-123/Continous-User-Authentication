using Newtonsoft.Json;
using System.IO;
namespace FYP;

public class ConfigurationService
{
    public AppConfig LoadConfiguration(string configFilePath)
    {
        var json = File.ReadAllText(configFilePath);
        return JsonConvert.DeserializeObject<AppConfig>(json);
    }
}

