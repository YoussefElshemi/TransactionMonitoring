using Core.Enums;

namespace Core.Constants;

public static class SandboxTriggerConstants
{
    public const string Prefix = "tm_test_";

    public static readonly IReadOnlyDictionary<string, SandboxTrigger> Triggers =
        new Dictionary<string, SandboxTrigger>(StringComparer.OrdinalIgnoreCase)
        {
            [Prefix + "allow"] = SandboxTrigger.Allow,
            [Prefix + "flag"] = SandboxTrigger.Flag,
            [Prefix + "review"] = SandboxTrigger.Review,
            [Prefix + "block"] = SandboxTrigger.Block,
            [Prefix + "hold"] = SandboxTrigger.Hold,
            [Prefix + "error"] = SandboxTrigger.Error
        };
}
