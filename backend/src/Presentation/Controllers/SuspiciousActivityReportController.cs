using System.Net.Mime;
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
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Mappers.SuspiciousActivityReports;
using Presentation.Models.Approvals;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Controllers;

[RequirePermission(Permission.SuspiciousActivityReportsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("suspicious-activity-reports")]
public class SuspiciousActivityReportController(
    IApplicationUserService applicationUserService,
    ISuspiciousActivityReportService suspiciousActivityReportService,
    IAuditService auditService,
    SuspiciousActivityReportResponseFactory suspiciousActivityReportResponseFactory,
    ApprovalResponseFactory approvalResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<ListSuspiciousActivityReportRequestDto> listSuspiciousActivityReportRequestValidator,
    IValidator<SuspiciousActivityReportRequestDto> suspiciousActivityReportRequestValidator,
    IValidator<UpdateSuspiciousActivityReportRequestDto> updateSuspiciousActivityReportRequestValidator,
    IValidator<RecomposeSuspiciousActivityReportRequestDto> recomposeSuspiciousActivityReportRequestValidator,
    IValidator<SubmitSuspiciousActivityReportRequestDto> submitSuspiciousActivityReportRequestValidator,
    IValidator<FileSuspiciousActivityReportRequestDto> fileSuspiciousActivityReportRequestValidator,
    IValidator<AcknowledgeSuspiciousActivityReportRequestDto> acknowledgeSuspiciousActivityReportRequestValidator,
    IValidator<DeclineSuspiciousActivityReportRequestDto> declineSuspiciousActivityReportRequestValidator,
    IValidator<ExportSuspiciousActivityReportRequestDto> exportSuspiciousActivityReportRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSuspiciousActivityReports(
        [FromQuery] ListSuspiciousActivityReportRequestDto listSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            listSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReports = await suspiciousActivityReportService.ListAsync(
            listSuspiciousActivityReportRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReports.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(suspiciousActivityReports.ToDto(resources));
    }

    [HttpGet("{SuspiciousActivityReportId:guid}")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuspiciousActivityReport(
        SuspiciousActivityReportRequestDto suspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        suspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await suspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            suspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.GetAsync(
            new SuspiciousActivityReportId(suspiciousActivityReportRequestDto.SuspiciousActivityReportId), HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsWrite)]
    [Idempotent]
    [HttpPatch("{SuspiciousActivityReportId:guid}")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSuspiciousActivityReport(
        UpdateSuspiciousActivityReportRequestDto updateSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            updateSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.UpdateNarrativeAsync(
            new UpdateSuspiciousActivityReportRequest
            {
                SuspiciousActivityReportId = new SuspiciousActivityReportId(
                    updateSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
                Narrative = new Narrative(updateSuspiciousActivityReportRequestDto.Body.Narrative)
            }, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsWrite)]
    [Idempotent]
    [HttpPost("{SuspiciousActivityReportId:guid}/narrative")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecomposeSuspiciousActivityReportNarrative(
        RecomposeSuspiciousActivityReportRequestDto recomposeSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recomposeSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recomposeSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            recomposeSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.RecomposeNarrativeAsync(
            new SuspiciousActivityReportId(recomposeSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
            HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsWrite)]
    [Idempotent]
    [HttpPost("{SuspiciousActivityReportId:guid}/submit")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SubmitSuspiciousActivityReport(
        SubmitSuspiciousActivityReportRequestDto submitSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        submitSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await submitSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            submitSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReportId = new SuspiciousActivityReportId(
            submitSuspiciousActivityReportRequestDto.SuspiciousActivityReportId);

        var approval = await suspiciousActivityReportService.SubmitAsync(new SubmitSuspiciousActivityReportRequest
        {
            SuspiciousActivityReportId = suspiciousActivityReportId,
            Reason = !string.IsNullOrWhiteSpace(submitSuspiciousActivityReportRequestDto.Body?.Reason)
                ? new Message(submitSuspiciousActivityReportRequestDto.Body.Reason)
                : null,
            SubmittedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        if (approval is not null)
        {
            var approvalResource = await approvalResponseFactory.CreateAsync(
                approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

            return Accepted(approvalResource.ToResponseData());
        }

        var suspiciousActivityReport = await suspiciousActivityReportService.GetAsync(
            suspiciousActivityReportId, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsFile)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{SuspiciousActivityReportId:guid}/file")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> FileSuspiciousActivityReport(
        FileSuspiciousActivityReportRequestDto fileSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        fileSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await fileSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            fileSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.FileAsync(new FileSuspiciousActivityReportRequest
        {
            SuspiciousActivityReportId = new SuspiciousActivityReportId(
                fileSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
            FilingReference = new FilingReference(fileSuspiciousActivityReportRequestDto.Body.FilingReference),
            FiledByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsFile)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{SuspiciousActivityReportId:guid}/acknowledge")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AcknowledgeSuspiciousActivityReport(
        AcknowledgeSuspiciousActivityReportRequestDto acknowledgeSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        acknowledgeSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await acknowledgeSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            acknowledgeSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.AcknowledgeAsync(
            new AcknowledgeSuspiciousActivityReportRequest
            {
                SuspiciousActivityReportId = new SuspiciousActivityReportId(
                    acknowledgeSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
                AcknowledgementReference = !string.IsNullOrWhiteSpace(
                    acknowledgeSuspiciousActivityReportRequestDto.Body?.AcknowledgementReference)
                    ? new FilingReference(acknowledgeSuspiciousActivityReportRequestDto.Body.AcknowledgementReference)
                    : null
            }, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsFile)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{SuspiciousActivityReportId:guid}/decline")]
    [ProducesResponseType<ResponseDataDto<SuspiciousActivityReportResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeclineSuspiciousActivityReport(
        DeclineSuspiciousActivityReportRequestDto declineSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        declineSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await declineSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            declineSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var suspiciousActivityReport = await suspiciousActivityReportService.DeclineAsync(
            new DeclineSuspiciousActivityReportRequest
            {
                SuspiciousActivityReportId = new SuspiciousActivityReportId(
                    declineSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
                Reason = new Message(declineSuspiciousActivityReportRequestDto.Body.Reason),
                DecidedByUserId = authenticatedUser.UserId
            }, HttpContext.RequestAborted);

        var resource = await suspiciousActivityReportResponseFactory.CreateAsync(
            suspiciousActivityReport, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.SuspiciousActivityReportsExport)]
    [HttpGet("{SuspiciousActivityReportId:guid}/export")]
    [Produces(MediaTypeNames.Application.Xml)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSuspiciousActivityReport(
        ExportSuspiciousActivityReportRequestDto exportSuspiciousActivityReportRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        exportSuspiciousActivityReportRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await exportSuspiciousActivityReportRequestValidator.ValidateAndThrowAsync(
            exportSuspiciousActivityReportRequestDto, HttpContext.RequestAborted);

        var export = await suspiciousActivityReportService.ExportAsync(
            new SuspiciousActivityReportId(exportSuspiciousActivityReportRequestDto.SuspiciousActivityReportId),
            HttpContext.RequestAborted);

        return File(export.Content, export.ContentType, export.FileName);
    }

    [HttpGet("{suspiciousActivityReportId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuspiciousActivityReportHistory(
        Guid suspiciousActivityReportId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.SuspiciousActivityReport, suspiciousActivityReportId),
            HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
