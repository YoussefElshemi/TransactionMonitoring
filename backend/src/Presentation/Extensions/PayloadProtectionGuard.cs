using Core.Configs;
using Core.Helpers;
using Environment = Core.Enums.Environment;

namespace Presentation.Extensions;

public static class PayloadProtectionGuard
{
    public static void Enforce(AppConfig appConfig)
    {
        var configured = appConfig.PayloadProtectionConfig.MasterKey;

        DataKeyWrapper.ReadMasterKey(configured);

        if (appConfig.Environment == Environment.Prod && DataKeyWrapper.IsDevelopmentMasterKey(configured))
        {
            throw new InvalidOperationException(
                "AppConfig:PayloadProtectionConfig:MasterKey is still the development key. " +
                "Production must supply its own, because the development key is published in the source repository " +
                "and every payload written under it is readable by anyone holding a copy.");
        }
    }
}
