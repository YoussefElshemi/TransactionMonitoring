using Core.Interfaces.Services;
using Core.Models.Notifications;
using Core.ValueObjects;
using Serilog;

namespace Infrastructure.Services;

public class LoggingEmailSender(ILogger logger) : IEmailSender
{
    public Task<Result> SendAsync(Destination destination, NotificationMessage notificationMessage, CancellationToken stoppingToken)
    {
        logger.Information(
            "Email notification for case {CaseId} to {Destination}: {Subject} — {Body}",
            notificationMessage.CaseId,
            destination.ToString(),
            notificationMessage.Subject,
            notificationMessage.Body);

        return Task.FromResult(new Result(true));
    }
}
