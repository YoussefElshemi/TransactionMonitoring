using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RuleSets;

namespace Presentation.Validators.RuleSets;

public class RuleSetRequestValidator : AbstractValidator<RuleSetRequestDto>
{
    public RuleSetRequestValidator(IRuleSetService ruleSetService)
    {
        RuleFor(x => x.RuleSetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleSetId(x.RuleSetId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule Set not found");
    }
}