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
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Mappers.Queues;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Queues;

namespace Presentation.Controllers;

[RequirePermission(Permission.QueuesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/queues")]
public class OrganizationQueueController(
    IApplicationUserService applicationUserService,
    IQueueService queueService,
    IAuditService auditService,
    QueueResponseFactory queueResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationListQueueRequestDto> listQueueRequestValidator,
    IValidator<CreateQueueRequestDto> createQueueRequestValidator,
    IValidator<OrganizationQueueRequestDto> organizationQueueRequestValidator,
    IValidator<UpdateQueueRequestDto> updateQueueRequestValidator,
    IValidator<SetQueueMembersRequestDto> setQueueMembersRequestValidator,
    IValidator<DeleteQueueRequestDto> deleteQueueRequestValidator,
    IValidator<RecoverQueueRequestDto> recoverQueueRequestValidator,
    IPermissionService permissionService) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<QueueResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListQueues([FromQuery] OrganizationListQueueRequestDto listQueueRequestDto)
    {
        await listQueueRequestValidator.ValidateAndThrowAsync(listQueueRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var permissionFilter = await permissionService.ResolveFilterAsync(
            User, Permission.QueuesRead, HttpContext.RequestAborted);

        var queues = await queueService.ListAsync(
            listQueueRequestDto.ToModel() with { PermissionFilter = permissionFilter }, HttpContext.RequestAborted);

        var resources = await queueResponseFactory.CreateAsync(queues.Data, context, HttpContext.RequestAborted);

        return Ok(queues.ToDto(resources));
    }

    [HttpGet("{QueueId:guid}")]
    [ProducesResponseType<ResponseDataDto<QueueResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue(OrganizationQueueRequestDto organizationQueueRequestDto)
    {
        await organizationQueueRequestValidator.ValidateAndThrowAsync(organizationQueueRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var queue = await queueService.GetAsync(new QueueId(organizationQueueRequestDto.QueueId), new IncludeDeleted(false), HttpContext.RequestAborted);

        var resource = await queueResponseFactory.CreateAsync(queue, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.QueuesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<QueueResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateQueue(CreateQueueRequestDto createQueueRequestDto)
    {
        await createQueueRequestValidator.ValidateAndThrowAsync(createQueueRequestDto, HttpContext.RequestAborted);

        var queue = await queueService.CreateAsync(createQueueRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, queue.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.QueuesWrite)]
    [Idempotent]
    [HttpPatch("{QueueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateQueue(UpdateQueueRequestDto updateQueueRequestDto)
    {
        await updateQueueRequestValidator.ValidateAndThrowAsync(updateQueueRequestDto, HttpContext.RequestAborted);

        await queueService.UpdateAsync(updateQueueRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{QueueId:guid}/members")]
    [ProducesResponseType<ResponseDataDto<QueueMemberResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListQueueMembers(OrganizationQueueRequestDto organizationQueueRequestDto)
    {
        await organizationQueueRequestValidator.ValidateAndThrowAsync(organizationQueueRequestDto, HttpContext.RequestAborted);

        var members = await queueService.ListMembersAsync(
            new QueueId(organizationQueueRequestDto.QueueId), HttpContext.RequestAborted);

        return Ok(members.Select(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.QueuesWrite)]
    [Idempotent]
    [HttpPut("{QueueId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetQueueMembers(SetQueueMembersRequestDto setQueueMembersRequestDto)
    {
        await setQueueMembersRequestValidator.ValidateAndThrowAsync(setQueueMembersRequestDto, HttpContext.RequestAborted);

        await queueService.SetMembersAsync(setQueueMembersRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("statistics")]
    [ProducesResponseType<ResponseDataDto<QueueStatisticResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueStatistics([FromQuery] OrganizationListQueueRequestDto listQueueRequestDto)
    {
        await listQueueRequestValidator.ValidateAndThrowAsync(listQueueRequestDto, HttpContext.RequestAborted);

        var statistics = await queueService.GetStatisticsAsync(
            new OrganizationId(listQueueRequestDto.OrganizationId), null, HttpContext.RequestAborted);

        return Ok(statistics.Select(x => x.ToDto()).ToArray().ToResponseData());
    }

    [HttpGet("{QueueId:guid}/statistics")]
    [ProducesResponseType<ResponseDataDto<QueueStatisticResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueStatistic(OrganizationQueueRequestDto organizationQueueRequestDto)
    {
        await organizationQueueRequestValidator.ValidateAndThrowAsync(organizationQueueRequestDto, HttpContext.RequestAborted);

        var statistics = await queueService.GetStatisticsAsync(
            new OrganizationId(organizationQueueRequestDto.OrganizationId),
            new QueueId(organizationQueueRequestDto.QueueId),
            HttpContext.RequestAborted);

        return Ok(statistics[0].ToDto().ToResponseData());
    }

    [HttpGet("{QueueId:guid}/usages")]
    [ProducesResponseType<ResponseDataDto<QueueUsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueUsages(OrganizationQueueRequestDto organizationQueueRequestDto)
    {
        await organizationQueueRequestValidator.ValidateAndThrowAsync(organizationQueueRequestDto, HttpContext.RequestAborted);

        var usage = await queueService.GetUsageAsync(
            new QueueId(organizationQueueRequestDto.QueueId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.QueuesWrite)]
    [Idempotent]
    [HttpDelete("{QueueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteQueue(DeleteQueueRequestDto deleteQueueRequestDto)
    {
        await deleteQueueRequestValidator.ValidateAndThrowAsync(deleteQueueRequestDto, HttpContext.RequestAborted);

        await queueService.DeleteAsync(new QueueId(deleteQueueRequestDto.QueueId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.QueuesWrite)]
    [Idempotent]
    [HttpPost("{QueueId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverQueue(RecoverQueueRequestDto recoverQueueRequestDto)
    {
        await recoverQueueRequestValidator.ValidateAndThrowAsync(recoverQueueRequestDto, HttpContext.RequestAborted);

        await queueService.RecoverAsync(new QueueId(recoverQueueRequestDto.QueueId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.QueuesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{queueId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueHistory(
        Guid organizationId,
        Guid queueId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Queue, queueId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
