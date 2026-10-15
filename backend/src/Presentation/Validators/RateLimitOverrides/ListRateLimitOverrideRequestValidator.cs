using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Validators.RateLimitOverrides;

public class ListRateLimitOverrideRequestValidator : AbstractValidator<ListRateLimitOverrideRequestDto>
{
    public ListRateLimitOverrideRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Pool)
            .IsEnumName(typeof(RateLimitPool), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Pool));
    }
}
