using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class CreateQueueRequestValidator : AbstractValidator<CreateQueueRequestDto>
{
    public CreateQueueRequestValidator(
        IOrganizationService organizationService,
        IQueueService queueService,
        IRuleService ruleService,
        IOrganizationUserService organizationUserService,
        ISubOrganizationService subOrganizationService)
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
            .MaximumLength(QueueName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await queueService.FindByNameAsync(new OrganizationId(x.OrganizationId), new QueueName(x.Body.Name), ct);

                return existing is null;
            })
            .WithMessage("Queue name is already declared for this organization");

        RuleFor(x => x.Body.Description)
            .MaximumLength(QueueDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Priority)
            .InclusiveBetween(0, QueuePriority.MaxValue)
            .When(x => x.Body.Priority.HasValue);

        RuleFor(x => x.Body.RoutingPolicy)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .IsEnumName(typeof(QueueRoutingPolicy), false);

        RuleFor(x => x.Body.MinimumSeverity)
            .IsEnumName(typeof(RuleSeverity), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.MinimumSeverity));

        RuleForEach(x => x.Body.RuleIds)
            .MustAsync(async (x, ruleId, ct) => await ruleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RuleId(ruleId), new IncludeDeleted(false), ct))
            .WithMessage("Rule not found")
            .When(x => x.Body.RuleIds is not null);

        RuleFor(x => x.Body.Matcher)
            .SetValidator(new QueueMatcherValidator()!)
            .When(x => x.Body.Matcher is not null);

        RuleFor(x => x.Body.MemberUserIds)
            .Must(x => x!.Distinct().Count() == x!.Length)
            .WithMessage("MemberUserIds cannot contain duplicates")
            .When(x => x.Body.MemberUserIds is not null);

        RuleForEach(x => x.Body.MemberUserIds)
            .MustAsync(async (x, userId, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(userId), new IncludeDeleted(false), ct))
            .WithMessage("User was not found in Organization")
            .When(x => x.Body.MemberUserIds is not null);

        RuleFor(x => x.Body.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.Body.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.Body.SubOrganizationId.HasValue);
    }
}
