using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.ExchangeRates;
using Presentation.Models.Common;
using Presentation.Models.ExchangeRates;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("exchange-rates")]
public class ExchangeRateController(
    IApplicationUserService applicationUserService,
    IExchangeRateService exchangeRateService,
    IValidator<CreateExchangeRateRequestDto> createExchangeRateRequestValidator,
    IValidator<ListExchangeRateRequestDto> listExchangeRateRequestValidator) : Controller
{
    [RequirePermission(Permission.ExchangeRatesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<ExchangeRateResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExchangeRate(CreateExchangeRateRequestDto createExchangeRateRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createExchangeRateRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createExchangeRateRequestValidator.ValidateAndThrowAsync(createExchangeRateRequestDto, HttpContext.RequestAborted);

        var exchangeRate = await exchangeRateService.CreateAsync(
            createExchangeRateRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, exchangeRate.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ExchangeRatesRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ExchangeRateResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExchangeRates([FromQuery] ListExchangeRateRequestDto listExchangeRateRequestDto)
    {
        await listExchangeRateRequestValidator.ValidateAndThrowAsync(listExchangeRateRequestDto, HttpContext.RequestAborted);

        var exchangeRates = await exchangeRateService.ListAsync(
            listExchangeRateRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(exchangeRates.ToDto(x => x.ToDto()));
    }
}
