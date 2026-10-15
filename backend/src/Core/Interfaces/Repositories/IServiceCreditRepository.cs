using Core.Models.ServiceCredits;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IServiceCreditRepository : IPagedRepository<ListServiceCreditRequest, ServiceCredit>
{
    Task AddAsync(ServiceCredit serviceCredit, CancellationToken stoppingToken);
    Task<ServiceCredit> GetAsync(ServiceCreditId serviceCreditId, CancellationToken stoppingToken);

    Task<Exists> ExistsByPeriodAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);
}
