using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISandboxCreditRepository
{
    // Locks until the transaction ends so concurrent callers cannot all read the same balance and overspend.
    Task LockAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
