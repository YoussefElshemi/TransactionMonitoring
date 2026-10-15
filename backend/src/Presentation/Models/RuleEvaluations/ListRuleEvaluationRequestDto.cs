using Presentation.Models.Common;

namespace Presentation.Models.RuleEvaluations;

public record ListRuleEvaluationRequestDto : PagedRequestDto
{
    public Guid? RuleId { get; init; }
}
