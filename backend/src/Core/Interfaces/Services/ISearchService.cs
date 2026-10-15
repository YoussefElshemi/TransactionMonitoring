using Core.Models.Search;

namespace Core.Interfaces.Services;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(SearchRequest searchRequest, CancellationToken stoppingToken);
}
