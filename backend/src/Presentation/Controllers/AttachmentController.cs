using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Attachments;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Attachments;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Models.Attachments;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
// Aliased: System.Net.Mime.ContentType collides with the value object.
using MediaTypeNames = System.Net.Mime.MediaTypeNames;

namespace Presentation.Controllers;

[RequirePermission(Permission.AttachmentsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("cases/{CaseId:guid}/attachments")]
public class AttachmentController(
    IApplicationUserService applicationUserService,
    IAttachmentService attachmentService,
    IAuditService auditService,
    AttachmentResponseFactory attachmentResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CreateAttachmentRequestDto> createAttachmentRequestValidator,
    IValidator<ListAttachmentRequestDto> listAttachmentsRequestValidator,
    IValidator<AttachmentRequestDto> attachmentRequestValidator,
    IValidator<DownloadAttachmentRequestDto> downloadAttachmentRequestValidator,
    IValidator<RescanAttachmentRequestDto> rescanAttachmentRequestValidator,
    IValidator<RecoverAttachmentRequestDto> recoverAttachmentRequestValidator) : Controller
{
    [RequirePermission(Permission.AttachmentsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<AttachmentResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAttachment([FromForm] CreateAttachmentRequestDto createAttachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createAttachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createAttachmentRequestValidator.ValidateAndThrowAsync(createAttachmentRequestDto, HttpContext.RequestAborted);

        await using var content = createAttachmentRequestDto.File.OpenReadStream();

        var createAttachmentRequest = new CreateAttachmentRequest
        {
            CaseId = new CaseId(createAttachmentRequestDto.CaseId),
            OrganizationId = authenticatedUser.OrganizationId,
            FileName = new FileName(createAttachmentRequestDto.File.FileName),
            ContentType = new ContentType(createAttachmentRequestDto.File.ContentType),
            Content = content,
            UploadedByUserId = authenticatedUser.UserId
        };

        var attachment = await attachmentService.CreateAsync(createAttachmentRequest, HttpContext.RequestAborted);

        var resource = await attachmentResponseFactory.CreateAsync(
            attachment, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<AttachmentResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAttachments([FromRoute] ListAttachmentRequestDto listAttachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAttachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listAttachmentsRequestValidator.ValidateAndThrowAsync(listAttachmentRequestDto, HttpContext.RequestAborted);

        var attachments = await attachmentService.ListAsync(listAttachmentRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await attachmentResponseFactory.CreateAsync(
            attachments.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(attachments.ToDto(resources));
    }

    [HttpGet("{AttachmentId:guid}")]
    [ProducesResponseType<ResponseDataDto<AttachmentResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttachment(AttachmentRequestDto attachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        attachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await attachmentRequestValidator.ValidateAndThrowAsync(attachmentRequestDto, HttpContext.RequestAborted);

        var attachment = await attachmentService.GetAsync(new AttachmentId(attachmentRequestDto.AttachmentId), HttpContext.RequestAborted);

        var resource = await attachmentResponseFactory.CreateAsync(
            attachment, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet("{AttachmentId:guid}/content")]
    [Produces(MediaTypeNames.Application.Octet)]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAttachment(DownloadAttachmentRequestDto downloadAttachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        downloadAttachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await downloadAttachmentRequestValidator.ValidateAndThrowAsync(downloadAttachmentRequestDto, HttpContext.RequestAborted);

        var attachmentContent = await attachmentService.DownloadAsync(
            new AttachmentId(downloadAttachmentRequestDto.AttachmentId), HttpContext.RequestAborted);

        return File(attachmentContent.Content, attachmentContent.Attachment.ContentType, attachmentContent.Attachment.FileName);
    }

    [RequirePermission(Permission.AttachmentsWrite)]
    [Idempotent]
    [HttpPost("{AttachmentId:guid}/rescan")]
    [ProducesResponseType<ResponseDataDto<AttachmentResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RescanAttachment(RescanAttachmentRequestDto rescanAttachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        rescanAttachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await rescanAttachmentRequestValidator.ValidateAndThrowAsync(rescanAttachmentRequestDto, HttpContext.RequestAborted);

        var attachment = await attachmentService.RescanAsync(
            new AttachmentId(rescanAttachmentRequestDto.AttachmentId), HttpContext.RequestAborted);

        var resource = await attachmentResponseFactory.CreateAsync(
            attachment, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.AttachmentsWrite)]
    [Idempotent]
    [HttpDelete("{AttachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAttachment(AttachmentRequestDto attachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        attachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await attachmentRequestValidator.ValidateAndThrowAsync(attachmentRequestDto, HttpContext.RequestAborted);

        await attachmentService.DeleteAsync(new AttachmentId(attachmentRequestDto.AttachmentId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.AttachmentsWrite)]
    [Idempotent]
    [HttpPost("{AttachmentId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverAttachment(RecoverAttachmentRequestDto recoverAttachmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverAttachmentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverAttachmentRequestValidator.ValidateAndThrowAsync(recoverAttachmentRequestDto, HttpContext.RequestAborted);

        await attachmentService.RecoverAsync(new AttachmentId(recoverAttachmentRequestDto.AttachmentId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{attachmentId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttachmentHistory(Guid attachmentId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Attachment, attachmentId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
