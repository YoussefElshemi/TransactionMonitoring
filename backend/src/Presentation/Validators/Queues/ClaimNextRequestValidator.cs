using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class ClaimNextRequestValidator : AbstractValidator<ClaimNextRequestDto>
{
    public ClaimNextRequestValidator(IQueueService queueService)
    {
        RuleFor(x => x.QueueId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(new OrganizationId(x.OrganizationId), new QueueId(x.QueueId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found")
            .MustAsync(async (x, _, ct) => await queueService.IsMemberAsync(new QueueId(x.QueueId!.Value), new ApplicationUserId(x.UserId), ct))
            .WithErrorCode(HttpStatusCode.Forbidden.ToString())
            .WithMessage("You are not a member of this queue")
            .When(x => x.QueueId.HasValue);
    }
}
