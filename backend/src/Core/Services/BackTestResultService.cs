using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.BackTests;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class BackTestResultService(
    IBackTestResultRepository backTestResultRepository) : IBackTestResultService
{
    public Task<PagedResponse<BackTestResult>> ListAsync(ListBackTestResultRequest request, CancellationToken stoppingToken)
    {
        return backTestResultRepository.ListAsync(request, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListBackTestResultRequest request, CancellationToken stoppingToken)
    {
        return backTestResultRepository.GetCountAsync(request, stoppingToken);
    }
}
