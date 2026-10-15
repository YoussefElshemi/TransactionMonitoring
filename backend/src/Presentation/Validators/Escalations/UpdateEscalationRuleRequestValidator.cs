using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Escalations;

namespace Presentation.Validators.Escalations;

public class UpdateEscalationRuleRequestValidator : AbstractValidator<UpdateEscalationRuleRequestDto>
{
    public UpdateEscalationRuleRequestValidator(
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

        RuleFor(x => x.EscalationRuleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await escalationRuleService.ExistsAsync(new OrganizationId(x.OrganizationId), new EscalationRuleId(x.EscalationRuleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Escalation rule not found");

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(EscalationRuleName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await escalationRuleService.FindByNameAsync(new OrganizationId(x.OrganizationId), new EscalationRuleName(x.Body.Name!), ct);

                return existing is null || existing.EscalationRuleId == x.EscalationRuleId;
            })
            .WithMessage("Escalation rule name is already declared for this organization")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Name));

        RuleFor(x => x.Body.Description)
            .MaximumLength(EscalationRuleDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Priority)
            .InclusiveBetween(0, EscalationPriority.MaxValue)
            .When(x => x.Body.Priority.HasValue);

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

        RuleFor(x => x.Body.ClearUnassignedFor)
            .Must(x => x != true)
            .WithMessage("ClearUnassignedFor cannot be sent alongside an UnassignedForMinutes")
            .When(x => x.Body.UnassignedForMinutes.HasValue);

        RuleFor(x => x.Body.ClearHoldExpiryWithin)
            .Must(x => x != true)
            .WithMessage("ClearHoldExpiryWithin cannot be sent alongside a HoldExpiryWithinMinutes")
            .When(x => x.Body.HoldExpiryWithinMinutes.HasValue);

        RuleFor(x => x.Body.ClearMinimumSeverity)
            .Must(x => x != true)
            .WithMessage("ClearMinimumSeverity cannot be sent alongside a MinimumSeverity")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity));

        RuleFor(x => x.Body.ClearMatcher)
            .Must(x => x != true)
            .WithMessage("ClearMatcher cannot be sent alongside a Matcher")
            .When(x => x.Body.Matcher is not null);

        RuleFor(x => x.Body.ClearTargetQueueId)
            .Must(x => x != true)
            .WithMessage("ClearTargetQueueId cannot be sent alongside a TargetQueueId")
            .When(x => x.Body.TargetQueueId.HasValue);

        RuleFor(x => x.Body.ClearRaiseSeverityTo)
            .Must(x => x != true)
            .WithMessage("ClearRaiseSeverityTo cannot be sent alongside a RaiseSeverityTo")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.RaiseSeverityTo));

        RuleFor(x => x.Body.Matcher)
            .SetValidator(new EscalationMatcherValidator()!)
            .When(x => x.Body.Matcher is not null);

        RuleFor(x => x.Body.TargetQueueId)
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(new OrganizationId(x.OrganizationId), new QueueId(x.Body.TargetQueueId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found")
            .When(x => x.Body.TargetQueueId.HasValue);

        RuleFor(x => x)
            .MustAsync(async (x, ct) =>
            {
                var existing = await escalationRuleService.GetAsync(new EscalationRuleId(x.EscalationRuleId), new IncludeDeleted(false), ct);

                var unassignedFor = x.Body.ClearUnassignedFor == true
                    ? false
                    : x.Body.UnassignedForMinutes.HasValue || existing.UnassignedFor.HasValue;

                var holdExpiryWithin = x.Body.ClearHoldExpiryWithin == true
                    ? false
                    : x.Body.HoldExpiryWithinMinutes.HasValue || existing.HoldExpiryWithin.HasValue;

                var minimumSeverity = x.Body.ClearMinimumSeverity == true
                    ? false
                    : !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity) || existing.MinimumSeverity.HasValue;

                var matcher = x.Body.ClearMatcher == true
                    ? false
                    : x.Body.Matcher is not null || existing.Matcher is not null;

                return unassignedFor || holdExpiryWithin || minimumSeverity || matcher;
            })
            .WithMessage("Escalation rule must declare at least one condition");
    }
}
