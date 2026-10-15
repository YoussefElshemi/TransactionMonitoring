using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Availability;

namespace Presentation.Validators.Availability;

public class DeleteAvailabilityCommitmentRequestValidator : AbstractValidator<DeleteAvailabilityCommitmentRequestDto>
{
    public DeleteAvailabilityCommitmentRequestValidator(
        IOrganizationService organizationService,
        IAvailabilityCommitmentService availabilityCommitmentService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, ct) => await availabilityCommitmentService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new AvailabilityCommitmentId(x.AvailabilityCommitmentId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Availability commitment not found");
    }
}
