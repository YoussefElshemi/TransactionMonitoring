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
[ValidateAuthenticatedUser]
[Route("audit-logs")]
public class AuditLogController(
    IApplicationUserService applicationUserService,
    IAuditService auditService,
    IValidator<ExportAuditLogRequestDto> exportAuditLogRequestValidator,
    IValidator<VerifyAuditChainRequestDto> verifyAuditChainRequestValidator) : Controller
{
    [RequirePermission(Permission.AuditLogsExport)]
    [HttpGet("export")]
    [Produces(AuditLogCsvDocument.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs([FromQuery] ExportAuditLogRequestDto exportAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        exportAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await exportAuditLogRequestValidator.ValidateAndThrowAsync(exportAuditLogRequestDto, HttpContext.RequestAborted);

        var export = await auditService.ExportAsync(exportAuditLogRequestDto.ToModel(), HttpContext.RequestAborted);

        return File(export.Content, export.ContentType, export.FileName);
    }

    [RequirePermission(Permission.AuditLogsVerify)]
    [HttpGet("verification")]
    [ProducesResponseType<ResponseDataDto<AuditChainVerificationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyAuditChain([FromQuery] VerifyAuditChainRequestDto verifyAuditChainRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        verifyAuditChainRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await verifyAuditChainRequestValidator.ValidateAndThrowAsync(verifyAuditChainRequestDto, HttpContext.RequestAborted);

        var verification = await auditService.VerifyAsync(verifyAuditChainRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(verification.ToDto().ToResponseData());
    }
}
