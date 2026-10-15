using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Availability;
using Presentation.Models.Availability;
using Presentation.Validators.Common;

namespace Presentation.Validators.Availability;

public class ListAvailabilityCommitmentRequestValidator(
    IOrganizationService organizationService,
    IAvailabilityCommitmentService availabilityCommitmentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListAvailabilityCommitmentRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListAvailabilityCommitmentRequestDto request, CancellationToken stoppingToken)
    {
        return availabilityCommitmentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
