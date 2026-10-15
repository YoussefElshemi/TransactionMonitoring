using Core.Configs;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class RecordComponentProbeRequestValidator : AbstractValidator<RecordComponentProbeRequestDto>
{
    public RecordComponentProbeRequestValidator(IOptions<AppConfig> appConfig, TimeProvider timeProvider)
    {
        var statusConfig = appConfig.Value.StatusConfig;

        RuleFor(x => x.Body.Component)
            .IsInEnum();

        RuleFor(x => x.Body.LatencyMilliseconds)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Body.Detail)
            .MaximumLength(ProbeDetail.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Detail));

        RuleFor(x => x.Body.ObservedAt)
            .LessThanOrEqualTo(_ => timeProvider.GetUtcNow())
            .When(x => x.Body.ObservedAt.HasValue)
            .WithMessage("ObservedAt cannot be in the future");

        RuleFor(x => x.Body.ObservedAt)
            .GreaterThanOrEqualTo(_ => timeProvider.GetUtcNow().AddDays(-statusConfig.ProbeRetentionDays))
            .When(x => x.Body.ObservedAt.HasValue)
            .WithMessage("ObservedAt cannot be older than the probe retention window");
    }
}
