using Core.Documents;
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
    "Every figure on the revenue board is composed across every customer on the installation at once. " +
    "Scoping it to one tenant would not answer a smaller version of the question, it would answer a " +
    "different one, and the permission behind it is never given to a customer.")]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("revenue")]
public class RevenueController(
    IRevenueService revenueService,
    IValidator<ListRecurringRevenueRequestDto> listRecurringRevenueRequestValidator,
    IValidator<GetRevenueConcentrationRequestDto> getRevenueConcentrationRequestValidator,
    IValidator<ListCustomerMarginRequestDto> listCustomerMarginRequestValidator,
    IValidator<GetRecoveryStatisticRequestDto> getRecoveryStatisticRequestValidator,
    IValidator<ListCustomerHealthRequestDto> listCustomerHealthRequestValidator,
    IValidator<ExportRevenueRecognitionRequestDto> exportRevenueRecognitionRequestValidator) : Controller
{
    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("recurring")]
    [ProducesResponseType<ResponseDataDto<List<RecurringRevenueMonthResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRecurringRevenue([FromQuery] ListRecurringRevenueRequestDto listRecurringRevenueRequestDto)
    {
        await listRecurringRevenueRequestValidator.ValidateAndThrowAsync(
            listRecurringRevenueRequestDto, HttpContext.RequestAborted);

        var months = await revenueService.ListRecurringRevenueAsync(
            listRecurringRevenueRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(months.ConvertAll(x => x.ToDto()).ToResponseData());
    }

    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("concentration")]
    [ProducesResponseType<ResponseDataDto<RevenueConcentrationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConcentration([FromQuery] GetRevenueConcentrationRequestDto getRevenueConcentrationRequestDto)
    {
        await getRevenueConcentrationRequestValidator.ValidateAndThrowAsync(
            getRevenueConcentrationRequestDto, HttpContext.RequestAborted);

        var concentration = await revenueService.GetConcentrationAsync(
            getRevenueConcentrationRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(concentration.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("margin")]
    [ProducesResponseType<ResponseDataDto<List<CustomerMarginResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMargins([FromQuery] ListCustomerMarginRequestDto listCustomerMarginRequestDto)
    {
        await listCustomerMarginRequestValidator.ValidateAndThrowAsync(
            listCustomerMarginRequestDto, HttpContext.RequestAborted);

        var margins = await revenueService.ListMarginsAsync(
            listCustomerMarginRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(margins.ConvertAll(x => x.ToDto()).ToResponseData());
    }

    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("recovery")]
    [ProducesResponseType<ResponseDataDto<RecoveryStatisticResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecovery([FromQuery] GetRecoveryStatisticRequestDto getRecoveryStatisticRequestDto)
    {
        await getRecoveryStatisticRequestValidator.ValidateAndThrowAsync(
            getRecoveryStatisticRequestDto, HttpContext.RequestAborted);

        var recovery = await revenueService.GetRecoveryAsync(
            getRecoveryStatisticRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(recovery.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("health")]
    [ProducesResponseType<ResponseDataDto<List<CustomerHealthResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListHealth([FromQuery] ListCustomerHealthRequestDto listCustomerHealthRequestDto)
    {
        await listCustomerHealthRequestValidator.ValidateAndThrowAsync(
            listCustomerHealthRequestDto, HttpContext.RequestAborted);

        var health = await revenueService.ListHealthAsync(
            listCustomerHealthRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(health.ConvertAll(x => x.ToDto()).ToResponseData());
    }

    [RequirePermission(Permission.RevenueRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("recognition")]
    [Produces(RevenueRecognitionCsvDocument.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportRecognition([FromQuery] ExportRevenueRecognitionRequestDto exportRevenueRecognitionRequestDto)
    {
        await exportRevenueRecognitionRequestValidator.ValidateAndThrowAsync(
            exportRevenueRecognitionRequestDto, HttpContext.RequestAborted);

        var exported = await revenueService.ExportRecognitionAsync(
            exportRevenueRecognitionRequestDto.ToModel(), HttpContext.RequestAborted);

        var fileName = $"revenue-recognition-" +
                       $"{exportRevenueRecognitionRequestDto.FromYear:D4}-{exportRevenueRecognitionRequestDto.FromMonth:D2}-to-" +
                       $"{exportRevenueRecognitionRequestDto.ToYear:D4}-{exportRevenueRecognitionRequestDto.ToMonth:D2}.csv";

        return File(exported.Content, RevenueRecognitionCsvDocument.ContentType, fileName);
    }
}
