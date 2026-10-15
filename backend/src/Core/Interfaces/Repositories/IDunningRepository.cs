using Core.Models.Dunning;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IDunningRepository : IPagedRepository<ListDunningStateRequest, DunningState>
{
    Task AddAsync(DunningState dunningState, CancellationToken stoppingToken);
    Task<DunningState> GetAsync(DunningStateId dunningStateId, CancellationToken stoppingToken);

    Task<DunningState?> FindOpenAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task<List<DunningState>> ListDueAsync(NextAttemptAt asOf, CancellationToken stoppingToken);

    Task AdvanceAsync(DunningState dunningState, CancellationToken stoppingToken);
    Task ResolveAsync(DunningStateId dunningStateId, ResolvedAt resolvedAt, CancellationToken stoppingToken);

    Task<List<DunningState>> ListOpenedBetweenAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<List<DunningState>> ListOpenAsync(CancellationToken stoppingToken);
}
