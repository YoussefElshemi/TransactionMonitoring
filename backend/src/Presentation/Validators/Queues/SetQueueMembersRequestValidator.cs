using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class SetQueueMembersRequestValidator : AbstractValidator<SetQueueMembersRequestDto>
{
    public SetQueueMembersRequestValidator(
        IOrganizationService organizationService,
        IQueueService queueService,
        IOrganizationUserService organizationUserService)
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

        RuleFor(x => x.Body.UserIds)
            .NotNull()
            .Must(x => x.Distinct().Count() == x.Length)
            .WithMessage("UserIds cannot contain duplicates");

        RuleForEach(x => x.Body.UserIds)
            .MustAsync(async (x, userId, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(userId), new IncludeDeleted(false), ct))
            .WithMessage("User was not found in Organization");
    }
}
