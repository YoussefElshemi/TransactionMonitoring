using System.Diagnostics;
using Core.Constants;

namespace Infrastructure.OpenTelemetry;

public static class OpenTelemetry
{
    public static readonly ActivitySource MyActivitySource = new(OpenTelemetryConstants.ServiceName);
}