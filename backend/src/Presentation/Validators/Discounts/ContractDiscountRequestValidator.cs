using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Discounts;

namespace Presentation.Validators.Discounts;

public class ContractDiscountRequestValidator : AbstractValidator<ContractDiscountRequestDto>
{
    public ContractDiscountRequestValidator(
        IOrganizationService organizationService,
        IContractDiscountService contractDiscountService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.ContractDiscountId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await contractDiscountService.ExistsAsync(new OrganizationId(dto.OrganizationId), new ContractDiscountId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Contract discount not found");
    }
}
