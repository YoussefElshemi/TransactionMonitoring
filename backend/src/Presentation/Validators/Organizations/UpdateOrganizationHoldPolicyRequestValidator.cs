using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Organizations;

namespace Presentation.Validators.Organizations;

public class UpdateOrganizationHoldPolicyRequestValidator : AbstractValidator<UpdateOrganizationHoldPolicyRequestDto>
{
    public UpdateOrganizationHoldPolicyRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.HoldWindow)
            .Must(holdWindow => holdWindow.Value > 0)
            .WithMessage("'Hold Window' must be greater than '0'.")
            .When(x => x.Body.HoldWindow is { IsSet: true, Value: not null });

        RuleFor(x => x.Body.HoldExpiryVerdict)
            .Must(holdExpiryVerdict => Enum.GetNames<HoldExpiryVerdict>().Contains(holdExpiryVerdict.Value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"HoldExpiryVerdict must be one of: {string.Join(", ", Enum.GetNames<HoldExpiryVerdict>())}")
            .When(x => x.Body.HoldExpiryVerdict is { IsSet: true, Value: not null });
    }
}
