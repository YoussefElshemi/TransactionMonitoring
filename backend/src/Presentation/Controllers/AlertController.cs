using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Alerts;
using Presentation.Mappers.Common;
using Presentation.Models.Alerts;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.AlertsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("alerts")]
public class AlertController(
    IApplicationUserService applicationUserService,
    IAlertService alertService,
    AlertResponseFactory alertResponseFactory,
    IValidator<AlertRequestDto> alertRequestValidator,
    IValidator<ListAlertRequestDto> listAlertRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<AlertResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAlerts([FromQuery] ListAlertRequestDto listAlertRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAlertRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listAlertRequestValidator.ValidateAndThrowAsync(listAlertRequestDto, HttpContext.RequestAborted);

        var alerts = await alertService.ListAsync(listAlertRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await alertResponseFactory.CreateAsync(
            alerts.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(alerts.ToDto(resources));
    }

    [HttpGet("{AlertId:guid}")]
    [ProducesResponseType<ResponseDataDto<AlertResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlert(AlertRequestDto alertRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        alertRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await alertRequestValidator.ValidateAndThrowAsync(alertRequestDto, HttpContext.RequestAborted);

        var alert = await alertService.GetAsync(new AlertId(alertRequestDto.AlertId), HttpContext.RequestAborted);

        var resource = await alertResponseFactory.CreateAsync(
            alert, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }
}
