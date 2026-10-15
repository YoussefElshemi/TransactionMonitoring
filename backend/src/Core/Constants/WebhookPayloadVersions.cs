using System.Collections.Frozen;

namespace Core.Constants;

public static class WebhookPayloadVersions
{
    public const int V1 = 1;

    public const int Current = V1;

    public static readonly FrozenSet<int> Supported = new[] { V1 }.ToFrozenSet();
}
