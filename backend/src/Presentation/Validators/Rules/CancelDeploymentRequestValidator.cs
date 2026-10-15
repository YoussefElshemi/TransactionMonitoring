using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class CancelDeploymentRequestValidator : AbstractValidator<CancelDeploymentRequestDto>
{
    public CancelDeploymentRequestValidator(IRuleService ruleService, IRuleDeploymentService ruleDeploymentService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found");

        RuleFor(x => x.DeploymentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleDeploymentService.DeploymentInProgressAsync(new OrganizationId(x.OrganizationId), new RuleDeploymentId(x.DeploymentId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("No in-progress deployment found");
    }
}
