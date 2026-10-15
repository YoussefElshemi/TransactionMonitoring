using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Approvals;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Approvals;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Models.Approvals;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.ApprovalsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("approvals")]
public class ApprovalController(
    IApplicationUserService applicationUserService,
    IApprovalService approvalService,
    IAuditService auditService,
    ApprovalResponseFactory approvalResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<ListApprovalRequestDto> listApprovalRequestValidator,
    IValidator<ApprovalRequestDto> approvalRequestValidator,
    IValidator<ApproveApprovalRequestDto> approveApprovalRequestValidator,
    IValidator<RejectApprovalRequestDto> rejectApprovalRequestValidator,
    IValidator<WithdrawApprovalRequestDto> withdrawApprovalRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApprovals([FromQuery] ListApprovalRequestDto listApprovalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listApprovalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listApprovalRequestValidator.ValidateAndThrowAsync(listApprovalRequestDto, HttpContext.RequestAborted);

        var approvals = await approvalService.ListAsync(listApprovalRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await approvalResponseFactory.CreateAsync(
            approvals.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(approvals.ToDto(resources));
    }

    [HttpGet("mine")]
    [ProducesResponseType<PagedResponseDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyApprovals([FromQuery] ListApprovalRequestDto listApprovalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listApprovalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        listApprovalRequestDto.RequestedByUserId = authenticatedUser.UserId;
        await listApprovalRequestValidator.ValidateAndThrowAsync(listApprovalRequestDto, HttpContext.RequestAborted);

        var approvals = await approvalService.ListAsync(listApprovalRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await approvalResponseFactory.CreateAsync(
            approvals.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(approvals.ToDto(resources));
    }

    [HttpGet("{ApprovalId:guid}")]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproval(ApprovalRequestDto approvalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        approvalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await approvalRequestValidator.ValidateAndThrowAsync(approvalRequestDto, HttpContext.RequestAborted);

        var approval = await approvalService.GetAsync(new ApprovalId(approvalRequestDto.ApprovalId), HttpContext.RequestAborted);

        var resource = await approvalResponseFactory.CreateAsync(
            approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.ApprovalsDecide)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{ApprovalId:guid}/approve")]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveApproval(ApproveApprovalRequestDto approveApprovalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        approveApprovalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await approveApprovalRequestValidator.ValidateAndThrowAsync(approveApprovalRequestDto, HttpContext.RequestAborted);

        var approval = await approvalService.ReviewAsync(new ReviewApprovalRequest
        {
            ApprovalId = new ApprovalId(approveApprovalRequestDto.ApprovalId),
            Status = ApprovalStatus.Approved,
            ReviewNote = !string.IsNullOrWhiteSpace(approveApprovalRequestDto.Body?.Note)
                ? new Message(approveApprovalRequestDto.Body.Note)
                : null,
            ReviewedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await approvalResponseFactory.CreateAsync(
            approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.ApprovalsDecide)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{ApprovalId:guid}/reject")]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectApproval(RejectApprovalRequestDto rejectApprovalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        rejectApprovalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await rejectApprovalRequestValidator.ValidateAndThrowAsync(rejectApprovalRequestDto, HttpContext.RequestAborted);

        var approval = await approvalService.ReviewAsync(new ReviewApprovalRequest
        {
            ApprovalId = new ApprovalId(rejectApprovalRequestDto.ApprovalId),
            Status = ApprovalStatus.Rejected,
            ReviewNote = new Message(rejectApprovalRequestDto.Body.Note!),
            ReviewedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await approvalResponseFactory.CreateAsync(
            approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [Idempotent]
    [HttpPost("{ApprovalId:guid}/withdraw")]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> WithdrawApproval(WithdrawApprovalRequestDto withdrawApprovalRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        withdrawApprovalRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await withdrawApprovalRequestValidator.ValidateAndThrowAsync(withdrawApprovalRequestDto, HttpContext.RequestAborted);

        var approval = await approvalService.ReviewAsync(new ReviewApprovalRequest
        {
            ApprovalId = new ApprovalId(withdrawApprovalRequestDto.ApprovalId),
            Status = ApprovalStatus.Withdrawn,
            ReviewNote = !string.IsNullOrWhiteSpace(withdrawApprovalRequestDto.Body?.Note)
                ? new Message(withdrawApprovalRequestDto.Body.Note)
                : null,
            ReviewedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await approvalResponseFactory.CreateAsync(
            approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet("{approvalId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalHistory(Guid approvalId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Approval, approvalId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
