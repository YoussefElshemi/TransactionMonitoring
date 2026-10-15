using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Discounts;

namespace Presentation.Validators.Discounts;

public class CreateContractDiscountRequestValidator : AbstractValidator<CreateContractDiscountRequestDto>
{
    public CreateContractDiscountRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.DiscountType)
            .IsInEnum();

        RuleFor(x => x.Body.Value)
            .GreaterThan(0)
            .WithMessage("A discount of nothing is not a discount");

        RuleFor(x => x.Body.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Body.DiscountType == DiscountType.Percentage)
            .WithMessage("A percentage discount cannot exceed 100");

        RuleFor(x => x.Body.Description)
            .NotEmpty()
            .MaximumLength(ContractDiscountDescription.MaxLength);

        RuleFor(x => x.Body.EffectiveTo)
            .GreaterThan(x => x.Body.EffectiveFrom ?? DateTimeOffset.MinValue)
            .When(x => x.Body.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom");
    }
}
