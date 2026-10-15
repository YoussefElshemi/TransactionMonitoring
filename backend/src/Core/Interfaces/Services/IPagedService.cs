using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPagedService<in TRequest, TModel>
    where TRequest : class
    where TModel : class
{
    Task<TotalCount> GetCountAsync(TRequest pagedRequest, CancellationToken stoppingToken);
    Task<PagedResponse<TModel>> ListAsync(TRequest pagedRequest, CancellationToken stoppingToken);
}