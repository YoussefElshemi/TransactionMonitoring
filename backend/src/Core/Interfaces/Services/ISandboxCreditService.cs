using Core.Enums;
using Core.Models.Sandbox;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ISandboxCreditService
{
    Task<SandboxCredit> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task EnforceAsync(OrganizationId organizationId, BillableResource billableResource, Resource resource, Count cost, CancellationToken stoppingToken);

    Task EnsureAvailableAsync(OrganizationId organizationId, Count cost, CancellationToken stoppingToken);
}
