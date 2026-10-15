using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Escalations;

namespace Presentation.Validators.Escalations;

public class CreateEscalationRuleRequestValidator : AbstractValidator<CreateEscalationRuleRequestDto>
{
    public CreateEscalationRuleRequestValidator(
        IOrganizationService organizationService,
        IEscalationRuleService escalationRuleService,
        IQueueService queueService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(EscalationRuleName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await escalationRuleService.FindByNameAsync(new OrganizationId(x.OrganizationId), new EscalationRuleName(x.Body.Name), ct);

                return existing is null;
            })
            .WithMessage("Escalation rule name is already declared for this organization");

        RuleFor(x => x.Body.Description)
            .MaximumLength(EscalationRuleDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Priority)
            .InclusiveBetween(0, EscalationPriority.MaxValue)
            .When(x => x.Body.Priority.HasValue);

        RuleFor(x => x.Body)
            .Must(x => x.UnassignedForMinutes.HasValue ||
                       x.HoldExpiryWithinMinutes.HasValue ||
                       !string.IsNullOrWhiteSpace(x.MinimumSeverity) ||
                       x.Matcher is not null)
            .WithMessage("Escalation rule must declare at least one condition");

        RuleFor(x => x.Body.UnassignedForMinutes)
            .InclusiveBetween(1, UnassignedFor.MaxValue)
            .When(x => x.Body.UnassignedForMinutes.HasValue);

        RuleFor(x => x.Body.HoldExpiryWithinMinutes)
            .InclusiveBetween(1, HoldExpiryWithin.MaxValue)
            .When(x => x.Body.HoldExpiryWithinMinutes.HasValue);

        RuleFor(x => x.Body.MinimumSeverity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity));

        RuleFor(x => x.Body.RaiseSeverityTo)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.RaiseSeverityTo));

        RuleFor(x => x.Body.Matcher)
            .SetValidator(new EscalationMatcherValidator()!)
            .When(x => x.Body.Matcher is not null);

        RuleFor(x => x.Body.TargetQueueId)
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(new OrganizationId(x.OrganizationId), new QueueId(x.Body.TargetQueueId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found")
            .When(x => x.Body.TargetQueueId.HasValue);
    }
}
