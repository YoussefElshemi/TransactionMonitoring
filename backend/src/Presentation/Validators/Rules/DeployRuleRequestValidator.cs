using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class DeployRuleRequestValidator : AbstractValidator<DeployRuleRequestDto>
{
    public DeployRuleRequestValidator(IRuleService ruleService, IRuleVersionService ruleVersionService, IRuleDeploymentService ruleDeploymentService)
    {
        RuleFor(x => x.RuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule not found")
            .MustAsync(async (x, _, ct) => !await ruleDeploymentService.ActiveDeploymentExistsAsync(new RuleId(x.RuleId), ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("A deployment is already in progress for this rule")

            .MustAsync(async (x, _, ct) => !await ruleDeploymentService.PendingApprovalExistsAsync(new RuleId(x.RuleId), ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("A deployment for this rule is already awaiting approval");

        RuleFor(x => x.Body.Strategy)
            .Must(strategy => Enum.TryParse<DeploymentStrategy>(strategy, true, out _))
            .WithMessage($"Strategy must be one of: {string.Join(", ", Enum.GetNames<DeploymentStrategy>())}");

        RuleFor(x => x.Body.VersionNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x)
            .MustAsync(async (x, _, ct) => await ruleVersionService.VersionExistsAsync(
                new OrganizationId(x.OrganizationId), new RuleId(x.RuleId), new VersionNumber(x.Body.VersionNumber), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule version not found")
            .When(x => x.Body.VersionNumber >= 1);

        When(IsGradual, () =>
        {
            RuleFor(x => x.Body.StepPercentage)
                .NotNull()
                .WithMessage("Step percentage is required for a gradual deployment")
                .InclusiveBetween(1, 100)
                .WithMessage("Step percentage must be between 1 and 100");

            RuleFor(x => x.Body.StepIntervalSeconds)
                .NotNull()
                .WithMessage("Step interval seconds is required for a gradual deployment")
                .GreaterThanOrEqualTo(1)
                .WithMessage("Step interval seconds must be greater than or equal to 1");
        });
    }

    private static bool IsGradual(DeployRuleRequestDto dto)
    {
        return Enum.TryParse<DeploymentStrategy>(dto.Body.Strategy, true, out var strategy) &&
               strategy == DeploymentStrategy.Gradual;
    }
}
