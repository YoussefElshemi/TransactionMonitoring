using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface INotificationService
{
    Task<Count> NotifyBreachedAsync(CancellationToken stoppingToken);
    Task<Count> NotifyOverdueFilingsAsync(CancellationToken stoppingToken);
}
