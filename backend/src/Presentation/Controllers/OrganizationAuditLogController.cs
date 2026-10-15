using Core.Documents;
using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.AuditLogsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/audit-logs")]
public class OrganizationAuditLogController(
    IAuditService auditService,
    IValidator<OrganizationExportAuditLogRequestDto> organizationExportAuditLogRequestValidator,
    IValidator<OrganizationVerifyAuditChainRequestDto> organizationVerifyAuditChainRequestValidator) : Controller
{
    [RequirePermission(Permission.AuditLogsExport)]
    [HttpGet("export")]
    [Produces(AuditLogCsvDocument.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] OrganizationExportAuditLogRequestDto organizationExportAuditLogRequestDto)
    {
        await organizationExportAuditLogRequestValidator.ValidateAndThrowAsync(
            organizationExportAuditLogRequestDto, HttpContext.RequestAborted);

        var export = await auditService.ExportAsync(organizationExportAuditLogRequestDto.ToModel(), HttpContext.RequestAborted);

        return File(export.Content, export.ContentType, export.FileName);
    }

    [RequirePermission(Permission.AuditLogsVerify)]
    [HttpGet("verification")]
    [ProducesResponseType<ResponseDataDto<AuditChainVerificationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyAuditChain(
        [FromQuery] OrganizationVerifyAuditChainRequestDto organizationVerifyAuditChainRequestDto)
    {
        await organizationVerifyAuditChainRequestValidator.ValidateAndThrowAsync(
            organizationVerifyAuditChainRequestDto, HttpContext.RequestAborted);

        var verification = await auditService.VerifyAsync(organizationVerifyAuditChainRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(verification.ToDto().ToResponseData());
    }
}
