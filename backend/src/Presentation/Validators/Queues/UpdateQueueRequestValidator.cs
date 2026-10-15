using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class UpdateQueueRequestValidator : AbstractValidator<UpdateQueueRequestDto>
{
    public UpdateQueueRequestValidator(
        IOrganizationService organizationService,
        IQueueService queueService,
        IRuleService ruleService,
        ISubOrganizationService subOrganizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.QueueId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(new OrganizationId(x.OrganizationId), new QueueId(x.QueueId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found");

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(QueueName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await queueService.FindByNameAsync(new OrganizationId(x.OrganizationId), new QueueName(x.Body.Name!), ct);

                return existing is null || existing.QueueId == x.QueueId;
            })
            .WithMessage("Queue name is already declared for this organization")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Name));

        RuleFor(x => x.Body.Description)
            .MaximumLength(QueueDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Priority)
            .InclusiveBetween(0, QueuePriority.MaxValue)
            .When(x => x.Body.Priority.HasValue);

        RuleFor(x => x.Body.RoutingPolicy)
            .IsEnumName(typeof(QueueRoutingPolicy), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.RoutingPolicy));

        RuleFor(x => x.Body.MinimumSeverity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity));

        RuleFor(x => x.Body.ClearMinimumSeverity)
            .Must(x => x != true)
            .WithMessage("ClearMinimumSeverity cannot be sent alongside a MinimumSeverity")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity));

        RuleFor(x => x.Body.ClearRuleIds)
            .Must(x => x != true)
            .WithMessage("ClearRuleIds cannot be sent alongside RuleIds")
            .When(x => x.Body.RuleIds is not null);

        RuleFor(x => x.Body.ClearMatcher)
            .Must(x => x != true)
            .WithMessage("ClearMatcher cannot be sent alongside a Matcher")
            .When(x => x.Body.Matcher is not null);

        RuleForEach(x => x.Body.RuleIds)
            .MustAsync(async (x, ruleId, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(ruleId), new IncludeDeleted(false), ct))
            .WithMessage("Rule not found")
            .When(x => x.Body.RuleIds is not null);

        RuleFor(x => x.Body.Matcher)
            .SetValidator(new QueueMatcherValidator()!)
            .When(x => x.Body.Matcher is not null);

        RuleFor(x => x.Body.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.Body.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.Body.SubOrganizationId.HasValue);

        RuleFor(x => x.Body.ClearSubOrganization)
            .Must(x => x != true)
            .WithMessage("ClearSubOrganization cannot be sent alongside a SubOrganizationId")
            .When(x => x.Body.SubOrganizationId.HasValue);
    }
}
