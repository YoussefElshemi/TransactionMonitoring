using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class RecoverQueueRequestValidator : AbstractValidator<RecoverQueueRequestDto>
{
    public RecoverQueueRequestValidator(IOrganizationService organizationService, IQueueService queueService)
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
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(new OrganizationId(x.OrganizationId), new QueueId(x.QueueId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found")
            .MustAsync(async (x, _, ct) =>
            {
                var retired = await queueService.GetAsync(new QueueId(x.QueueId), new IncludeDeleted(true), ct);
                var holder = await queueService.FindByNameAsync(new OrganizationId(x.OrganizationId), retired.Name, ct);

                return holder is null;
            })
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("Queue name is already declared for this organization");
    }
}
