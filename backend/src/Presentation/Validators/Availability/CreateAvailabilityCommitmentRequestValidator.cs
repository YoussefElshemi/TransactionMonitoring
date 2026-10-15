using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Availability;

namespace Presentation.Validators.Availability;

public class CreateAvailabilityCommitmentRequestValidator : AbstractValidator<CreateAvailabilityCommitmentRequestDto>
{
    public CreateAvailabilityCommitmentRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.AvailabilityTarget)
            .GreaterThan(0)
            .LessThan(100)
            .WithMessage("An availability target must be above zero and below one hundred per cent");

        RuleFor(x => x.Body.LatencyTargetMilliseconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(LatencyTarget.MaxValue);

        RuleFor(x => x.Body.LatencyPercentile)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Body.EffectiveTo)
            .GreaterThan(x => x.Body.EffectiveFrom)
            .When(x => x.Body.EffectiveFrom.HasValue && x.Body.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom");

        RuleForEach(x => x.Body.CreditBands)
            .ChildRules(band =>
            {
                band.RuleFor(x => x.BelowUptimePercentage)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(100);

                band.RuleFor(x => x.CreditPercentage)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(100);
            });

        RuleFor(x => x.Body.CreditBands)
            .Must(x => x.Select(band => band.BelowUptimePercentage).Distinct().Count() == x.Count)
            .WithMessage("Two credit bands cannot share a threshold");

        RuleFor(x => x.Body.CreditBands)
            .Must((request, bands) => bands.All(band => band.BelowUptimePercentage <= request.Body.AvailabilityTarget))
            .WithMessage("A credit band cannot sit above the availability target");
    }
}
