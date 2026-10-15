using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Models.Common;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Controllers;

[RequirePermission(Permission.SuspiciousActivityReportsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("cases/{CaseId:guid}/suspicious-activity-report")]
public class CaseSuspiciousActivityReportController(
    IApplicationUserService applicationUserService,
    ISuspiciousActivityReportService suspiciousActivityReportService,
    SuspiciousActivityReportResponseFactory suspiciousActivityReportResponseFactory,
    IValidator<GetCaseSuspiciousActivityReportRequestDto> getCaseSuspiciousActivityReportRequestValidator,
    IValidator<OpenSuspiciousActivityReportRequestDto> openSuspiciousActivityReportRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaseSuspiciousActivityReport(
        GetCaseSuspiciousActivityReportRequestDto getCaseSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        getCaseSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await getCaseSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            getCaseSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.FindByCaseAsync(
            new CaseId(getCaseSuspiciousActivityReportRequestDto.CaseId), HttpContext.RequestAborted);

        if (suspiciousActivityReport is null)
        {
            return NotFound();
        }

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> OpenSuspiciousActivityReport(
        OpenSuspiciousActivityReportRequestDto openSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        openSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await openSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            openSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.OpenAsync(
            new OpenSuspiciousActivityReportRequest
            {
                OrganizationId = authenticatedUser.OrganizationId,
                CaseId = new CaseId(openSuspiciousActivityReportRequestDto.CaseId),
                Narrative = !string.IsNullOrWhiteSpace(openSuspiciousActivityReportRequestDto.Body?.Narrative)
                    ? new Narrative(openSuspiciousActivityReportRequestDto.Body.Narrative)
                    : null,
                OpenedByUserId = authenticatedUser.UserId
            }, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }
}
