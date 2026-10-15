namespace Core.Configs;

public record PayloadProtectionConfig
{
    public string MasterKey { get; init; } = DevelopmentMasterKey;

    public const string DevelopmentMasterKey = "ZGV2ZWxvcG1lbnQtb25seS1tYXN0ZXIta2V5LTMyISE=";
}
