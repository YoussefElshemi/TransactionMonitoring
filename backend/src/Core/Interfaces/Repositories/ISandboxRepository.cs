using Core.Models.Sandbox;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISandboxRepository
{
    Task<List<SandboxResetCount>> ResetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
