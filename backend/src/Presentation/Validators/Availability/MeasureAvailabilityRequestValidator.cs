using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Availability;

namespace Presentation.Validators.Availability;

public class MeasureAvailabilityRequestValidator : AbstractValidator<MeasureAvailabilityRequestDto>
{
    public MeasureAvailabilityRequestValidator(IOrganizationService organizationService, TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("From Date cannot be in the future.");

        RuleFor(x => new { x.FromDate, x.ToDate })
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("From Date must be before or equal to To Date.");
    }
}
