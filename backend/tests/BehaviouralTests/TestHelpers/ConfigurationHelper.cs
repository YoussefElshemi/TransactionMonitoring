using Microsoft.Extensions.Configuration;

namespace BehaviouralTests.TestHelpers;

public static class ConfigurationHelper
{
    private const string SettingsFile = "appsettings.Testing.json";

    public static readonly IConfigurationRoot Config =
        new ConfigurationBuilder()
            .AddJsonFile(SettingsFile, false)
            .AddEnvironmentVariables()
            .Build();

    internal static IConfigurationSection GetSection(string connectionString)
    {
        return Config.GetSection(connectionString);
    }
}