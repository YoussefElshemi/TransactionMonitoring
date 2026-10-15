using System.Net;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Validators.RateLimitOverrides;

public class CreateRateLimitOverrideRequestValidator : AbstractValidator<CreateRateLimitOverrideRequestDto>
{
    public CreateRateLimitOverrideRequestValidator(
        IOrganizationService organizationService,
        IOptions<AppConfig> appConfig,
        TimeProvider timeProvider)
    {
        var maximumDuration = TimeSpan.FromDays(appConfig.Value.RateLimitConfig.MaximumOverrideDurationDays);

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Pool)
            .IsEnumName(typeof(RateLimitPool), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Pool));

        RuleFor(x => x.Body.PermitLimit)
            .GreaterThan(0);

        RuleFor(x => x.Body.TokensPerPeriod)
            .GreaterThan(0);

        RuleFor(x => x.Body.ReplenishmentPeriodSeconds)
            .GreaterThan(0);

        RuleFor(x => x.Body.Reason)
            .NotEmpty()
            .MaximumLength(RateLimitOverrideReason.MaxLength);

        RuleFor(x => x.Body.ExpiresAt)
            .Cascade(CascadeMode.Stop)
            .Must(x => x > timeProvider.GetUtcNow())
            .WithMessage("ExpiresAt must be in the future")
            .Must(x => x <= timeProvider.GetUtcNow().Add(maximumDuration))
            .WithMessage($"ExpiresAt must be within {maximumDuration.TotalDays} days. Renew the override rather than extending it indefinitely.");
    }
}
