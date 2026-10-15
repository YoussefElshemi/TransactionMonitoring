using Core.Models.BackTests;

namespace Core.Interfaces.Repositories;

public interface IBackTestResultRepository : IPagedRepository<ListBackTestResultRequest, BackTestResult>
{
    Task AddRangeAsync(IReadOnlyList<BackTestResult> results, CancellationToken stoppingToken);
}
