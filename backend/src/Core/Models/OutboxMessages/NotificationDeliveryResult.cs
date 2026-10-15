using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record NotificationDeliveryResult
{
    public required Result Result { get; init; }

    public required StatusCode? StatusCode { get; init; }

    public required ResponseBody? ResponseBody { get; init; }

    public required LatencyMilliseconds Latency { get; init; }

    public static NotificationDeliveryResult Failed(LatencyMilliseconds latency, string? error = null)
    {
        return new NotificationDeliveryResult
        {
            Result = new Result(false),
            StatusCode = null,
            ResponseBody = error is null ? null : new ResponseBody(error),
            Latency = latency
        };
    }
}
