using System.Net;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.VelocityProfiles;

namespace Presentation.Validators.VelocityProfiles;

public class SetVelocityProfileRequestValidator : AbstractValidator<SetVelocityProfileRequestDto>
{
    internal const int MaxMetrics = 25;
    internal const int MaxThresholds = 25;

    public SetVelocityProfileRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Metrics)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A velocity profile must declare at least one metric.")
            .Must(x => x.Count <= MaxMetrics)
            .WithMessage($"A velocity profile must not declare more than {MaxMetrics} metrics.")
            .Must(x => x.Select(metric => metric.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count)
            .WithMessage("A metric must not be declared twice.")
            .DependentRules(() => { RuleForEach(x => x.Body.Metrics).SetValidator(new VelocityMetricValidator()); });

        RuleFor(x => x.Body.Thresholds!)
            .Cascade(CascadeMode.Stop)
            .Must(x => x.Count <= MaxThresholds)
            .WithMessage($"A velocity profile must not declare more than {MaxThresholds} thresholds.")
            .Must(x => x.Select(threshold => threshold.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count)
            .WithMessage("A threshold must not be declared twice.")
            .When(x => x.Body.Thresholds != null)
            .DependentRules(() => { RuleForEach(x => x.Body.Thresholds).SetValidator(new VelocityThresholdValidator()); });
    }
}

public class VelocityMetricValidator : AbstractValidator<VelocityMetricDto>
{
    public VelocityMetricValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A metric must be named, because that is how a rule addresses it.")
            .MaximumLength(VelocityMetricName.MaxLength);

        RuleFor(x => x.Path)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A metric must state where the payment carries it.")
            .Must(PayloadPath.IsAddressable)
            .WithMessage(x => $"'{x.Path}' is not a readable field path.")
            .Must(x => !PayloadPath.HasWildcard(x))
            .WithMessage(x => $"'{x.Path}' resolves to many values, so it cannot be a metric.");
    }
}

public class VelocityThresholdValidator : AbstractValidator<VelocityThresholdDto>
{
    public VelocityThresholdValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A threshold must be named, because that is how a rule addresses it.")
            .MaximumLength(VelocityThresholdName.MaxLength);

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("A threshold must be greater than zero, or nothing can sit under it.");
    }
}
