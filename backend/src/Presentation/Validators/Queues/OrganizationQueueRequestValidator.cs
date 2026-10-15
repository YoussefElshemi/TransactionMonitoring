using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class OrganizationQueueRequestValidator : AbstractValidator<OrganizationQueueRequestDto>
{
    public OrganizationQueueRequestValidator(IOrganizationService organizationService, IQueueService queueService)
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
    }
}
