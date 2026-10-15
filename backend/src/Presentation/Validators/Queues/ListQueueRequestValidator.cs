using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Queues;

namespace Presentation.Validators.Queues;

public class ListQueueRequestValidator : AbstractValidator<OrganizationListQueueRequestDto>
{
    public ListQueueRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Name)
            .MaximumLength(QueueName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.RoutingPolicy)
            .IsEnumName(typeof(QueueRoutingPolicy), false)
            .When(x => !string.IsNullOrWhiteSpace(x.RoutingPolicy));
    }
}
