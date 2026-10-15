using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Revenue;
using Presentation.Models.Common;
using Presentation.Models.Revenue;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[OrganizationScopeExempt(
    "A cost to serve is the installation's own figure and belongs to no tenant. It is read across every " +
    "customer at once by the margin board and is never scoped to one.")]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("service-cost-rates")]
public class ServiceCostRateController(
    IApplicationUserService applicationUserService,
    IServiceCostRateService serviceCostRateService,
    IValidator<CreateServiceCostRateRequestDto> createServiceCostRateRequestValidator,
    IValidator<ListServiceCostRateRequestDto> listServiceCostRateRequestValidator) : Controller
{
    [RequirePermission(Permission.ServiceCostRatesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<ServiceCostRateResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateServiceCostRate(CreateServiceCostRateRequestDto createServiceCostRateRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createServiceCostRateRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createServiceCostRateRequestValidator.ValidateAndThrowAsync(createServiceCostRateRequestDto, HttpContext.RequestAborted);

        var serviceCostRate = await serviceCostRateService.CreateAsync(
            createServiceCostRateRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, serviceCostRate.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ServiceCostRatesRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ServiceCostRateResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListServiceCostRates([FromQuery] ListServiceCostRateRequestDto listServiceCostRateRequestDto)
    {
        await listServiceCostRateRequestValidator.ValidateAndThrowAsync(listServiceCostRateRequestDto, HttpContext.RequestAborted);

        var serviceCostRates = await serviceCostRateService.ListAsync(
            listServiceCostRateRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(serviceCostRates.ToDto(x => x.ToDto()));
    }
}
