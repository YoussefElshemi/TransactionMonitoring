using System.Diagnostics;
using Core.Constants;

namespace Core.OpenTelemetry;

public static class OpenTelemetry
{
    public static readonly ActivitySource MyActivitySource = new(OpenTelemetryConstants.ServiceName);
}
