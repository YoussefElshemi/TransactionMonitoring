using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.RuleEvaluations;
using Presentation.Validators.Common;

namespace Presentation.Validators.RuleEvaluations;

public class ListRuleEvaluationRequestValidator(
    IRuleEvaluationService ruleEvaluationService,
    TimeProvider timeProvider) : PagedRequestValidator<ListRuleEvaluationRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListRuleEvaluationRequestDto request, CancellationToken stoppingToken)
    {
        return ruleEvaluationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
