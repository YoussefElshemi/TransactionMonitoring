using Core.Enums;
using Core.Interfaces.Services;
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
[Route("cases/{RouteCaseId:guid}/alerts")]
public class CaseAlertController(
    IApplicationUserService applicationUserService,
    IAlertService alertService,
    AlertResponseFactory alertResponseFactory,
    IValidator<ListCaseAlertRequestDto> listCaseAlertRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<AlertResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCaseAlerts([FromQuery] ListCaseAlertRequestDto listCaseAlertRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listCaseAlertRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        listCaseAlertRequestDto.CaseId = listCaseAlertRequestDto.RouteCaseId;

        await listCaseAlertRequestValidator.ValidateAndThrowAsync(listCaseAlertRequestDto, HttpContext.RequestAborted);

        var alerts = await alertService.ListAsync(listCaseAlertRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await alertResponseFactory.CreateAsync(
            alerts.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(alerts.ToDto(resources));
    }
}
