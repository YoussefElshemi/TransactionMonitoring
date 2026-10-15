using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.SuspiciousActivityReports;
using Presentation.Models.Common;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Controllers;

[RequirePermission(Permission.SuspiciousActivityReportPoliciesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/suspicious-activity-report-policy")]
public class OrganizationSuspiciousActivityReportPolicyController(
    ISuspiciousActivityReportService suspiciousActivityReportService,
    IOptions<AppConfig> appConfig,
    IValidator<OrganizationSuspiciousActivityReportPolicyRequestDto> organizationSuspiciousActivityReportPolicyRequestValidator,
    IValidator<SetOrganizationSuspiciousActivityReportPolicyRequestDto> setOrganizationSuspiciousActivityReportPolicyRequestValidator)
    : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportPolicyResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuspiciousActivityReportPolicy(
        OrganizationSuspiciousActivityReportPolicyRequestDto organizationSuspiciousActivityReportPolicyRequestDto)
    {
        await organizationSuspiciousActivityReportPolicyRequestValidator.ValidateAndThrowAsync(
            organizationSuspiciousActivityReportPolicyRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReportPolicy = await suspiciousActivityReportService.FindPolicyAsync(
            new OrganizationId(organizationSuspiciousActivityReportPolicyRequestDto.OrganizationId), HttpContext.RequestAborted);

        if (suspiciousActivityReportPolicy is null)
        {
            return NotFound();
        }

        return Ok(suspiciousActivityReportPolicy.ToDto(appConfig.Value.SuspiciousActivityReportConfig).ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportPoliciesWrite)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSuspiciousActivityReportPolicy(
        SetOrganizationSuspiciousActivityReportPolicyRequestDto setOrganizationSuspiciousActivityReportPolicyRequestDto)
    {
        await setOrganizationSuspiciousActivityReportPolicyRequestValidator.ValidateAndThrowAsync(
            setOrganizationSuspiciousActivityReportPolicyRequestDto, HttpContext.RequestAborted);

        await suspiciousActivityReportService.SetPolicyAsync(
            setOrganizationSuspiciousActivityReportPolicyRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
