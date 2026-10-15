using Core.Models.ServiceCredits;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IServiceCreditService : IPagedService<ListServiceCreditRequest, ServiceCredit>
{
    Task<ServiceCredit> IssueAsync(IssueServiceCreditRequest issueServiceCreditRequest, CancellationToken stoppingToken);

    Task<ServiceCredit> GetAsync(ServiceCreditId serviceCreditId, CancellationToken stoppingToken);
    Task<Exists> ExistsByPeriodAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);
}
