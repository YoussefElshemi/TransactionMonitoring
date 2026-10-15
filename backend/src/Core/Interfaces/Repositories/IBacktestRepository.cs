using Core.Models.BackTests;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IBackTestRepository : IPagedRepository<ListBackTestRequest, BackTest>
{
    Task AddAsync(BackTest backTest, CancellationToken stoppingToken);
    Task<BackTest> GetAsync(BackTestId backTestId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, BackTestId backTestId, CancellationToken stoppingToken);
    Task<Exists> ExistsActiveAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateBackTestRequest request, CancellationToken stoppingToken);
}
