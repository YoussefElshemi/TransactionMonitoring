using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.CaseLinks;
using Presentation.Models.CaseLinks;
using Presentation.Validators.Common;

namespace Presentation.Validators.CaseLinks;

public class ListCaseLinkRequestValidator(
    ICaseLinkService caseLinkService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCaseLinkRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListCaseLinkRequestDto request, CancellationToken stoppingToken)
    {
        return caseLinkService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
