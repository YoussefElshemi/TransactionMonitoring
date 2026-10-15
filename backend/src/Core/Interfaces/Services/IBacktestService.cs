using Core.Models.BackTests;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IBackTestService : IPagedService<ListBackTestRequest, BackTest>
{
    Task<BackTest> CreateAsync(CreateBackTestRequest request, CancellationToken stoppingToken);
    Task<BackTest> GetAsync(BackTestId backTestId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, BackTestId backTestId, CancellationToken stoppingToken);
    Task<Exists> ExistsActiveAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken);
}
