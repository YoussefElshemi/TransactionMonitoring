using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.RuleSets;
using Presentation.Models.RuleSets;
using Presentation.Validators.Common;

namespace Presentation.Validators.RuleSets;

public class ListRuleSetRequestValidator(
    IRuleSetService ruleSetService,
    TimeProvider timeProvider) : PagedRequestValidator<ListRuleSetRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListRuleSetRequestDto request, CancellationToken stoppingToken)
    {
        return ruleSetService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}