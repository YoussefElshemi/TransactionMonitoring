using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Validators.RateLimitOverrides;

public class DeleteRateLimitOverrideRequestValidator : AbstractValidator<DeleteRateLimitOverrideRequestDto>
{
    public DeleteRateLimitOverrideRequestValidator(
        IOrganizationService organizationService,
        IRateLimitOverrideService rateLimitOverrideService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.RateLimitOverrideId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await rateLimitOverrideService.ExistsAsync(new OrganizationId(dto.OrganizationId), new RateLimitOverrideId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rate limit override not found");
    }
}
