using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Validators.RuleEvaluations;

public class RuleEvaluationRequestValidator : AbstractValidator<RuleEvaluationRequestDto>
{
    public RuleEvaluationRequestValidator(IRuleEvaluationService ruleEvaluationService)
    {
        RuleFor(x => x.RuleEvaluationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleEvaluationService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleEvaluationId(x.RuleEvaluationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Evaluation not found");
    }
}
