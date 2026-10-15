using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPagedRepository<in TRequest, TModel>
    where TRequest : class
    where TModel : class
{
    Task<TotalCount> GetCountAsync(TRequest listInvoiceRequest, CancellationToken stoppingToken);
    Task<PagedResponse<TModel>> ListAsync(TRequest listInvoiceRequest, CancellationToken stoppingToken);
}