using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Discounts;
using Presentation.Models.Discounts;
using Presentation.Validators.Common;

namespace Presentation.Validators.Discounts;

public class ListContractDiscountRequestValidator(
    IOrganizationService organizationService,
    IContractDiscountService contractDiscountService,
    TimeProvider timeProvider) : PagedRequestValidator<ListContractDiscountRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListContractDiscountRequestDto request, CancellationToken stoppingToken)
    {
        return contractDiscountService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
