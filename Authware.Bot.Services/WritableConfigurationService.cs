using System.Text.Json;
using Authware.Bot.Services.Interfaces;

namespace Authware.Bot.Services;

public class WritableConfigurationService<TConfiguration> : IWritableConfigurationService<TConfiguration>
{
    private const string ConfigurationPath = "authware.json";

    public void Save(TConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration);
        File.WriteAllText(ConfigurationPath, json);
    }

    public TConfiguration Load()
    {
        var json = File.ReadAllText(ConfigurationPath);
        var o = JsonSerializer.Deserialize<TConfiguration>(json);

        return o;
    }
}