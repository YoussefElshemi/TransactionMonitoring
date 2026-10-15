using Core.Models.Dunning;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IDunningService : IPagedService<ListDunningStateRequest, DunningState>
{
    Task<Count> ReconcileAsync(CancellationToken stoppingToken);

    Task<Count> AdvanceAsync(CancellationToken stoppingToken);

    Task<DunningState> GetAsync(DunningStateId dunningStateId, CancellationToken stoppingToken);
}
