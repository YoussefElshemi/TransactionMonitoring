using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.NotificationSubscriptions;
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
using Presentation.Mappers.NotificationSubscriptions;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Controllers;

[RequirePermission(Permission.NotificationSubscriptionsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("notification-subscriptions")]
public class NotificationSubscriptionController(
    IApplicationUserService applicationUserService,
    INotificationSubscriptionService notificationSubscriptionService,
    IAuditService auditService,
    NotificationSubscriptionResponseFactory notificationSubscriptionResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CreateNotificationSubscriptionRequestDto> createNotificationSubscriptionRequestValidator,
    IValidator<ListNotificationSubscriptionRequestDto> listNotificationSubscriptionsRequestValidator,
    IValidator<NotificationSubscriptionRequestDto> notificationSubscriptionRequestValidator,
    IValidator<UpdateNotificationSubscriptionRequestDto> updateNotificationSubscriptionRequestValidator,
    IValidator<DeleteNotificationSubscriptionRequestDto> deleteNotificationSubscriptionRequestValidator) : Controller
{
    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNotificationSubscription(CreateNotificationSubscriptionRequestDto createNotificationSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createNotificationSubscriptionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(createNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var createNotificationSubscriptionRequest = new CreateNotificationSubscriptionRequest
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Channel = createNotificationSubscriptionRequestDto.Body.Channel,
            Destination = new Destination(createNotificationSubscriptionRequestDto.Body.Destination),
            MinimumSeverity = createNotificationSubscriptionRequestDto.Body.MinimumSeverity,
            NotifyOnBreach = new NotifyOnBreach(createNotificationSubscriptionRequestDto.Body.NotifyOnBreach),
            IsEnabled = new IsEnabled(createNotificationSubscriptionRequestDto.Body.IsEnabled),
            Locale = string.IsNullOrWhiteSpace(createNotificationSubscriptionRequestDto.Body.Locale)
                ? null
                : new Locale(createNotificationSubscriptionRequestDto.Body.Locale)
        };

        var notificationSubscription = await notificationSubscriptionService.CreateAsync(createNotificationSubscriptionRequest, HttpContext.RequestAborted);

        var resource = notificationSubscriptionResponseFactory.Create(notificationSubscription);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotificationSubscriptions([FromQuery] ListNotificationSubscriptionRequestDto listNotificationSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listNotificationSubscriptionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listNotificationSubscriptionsRequestValidator.ValidateAndThrowAsync(listNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var notificationSubscriptions = await notificationSubscriptionService.ListAsync(listNotificationSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = notificationSubscriptionResponseFactory.Create(notificationSubscriptions.Data);

        return Ok(notificationSubscriptions.ToDto(resources));
    }

    [HttpGet("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType<ResponseDataDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationSubscription(NotificationSubscriptionRequestDto notificationSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        notificationSubscriptionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await notificationSubscriptionRequestValidator.ValidateAndThrowAsync(notificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var notificationSubscription = await notificationSubscriptionService.GetAsync(
            new NotificationSubscriptionId(notificationSubscriptionRequestDto.NotificationSubscriptionId), HttpContext.RequestAborted);

        var resource = notificationSubscriptionResponseFactory.Create(notificationSubscription);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpPatch("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationSubscription(UpdateNotificationSubscriptionRequestDto updateNotificationSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateNotificationSubscriptionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(updateNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        await notificationSubscriptionService.UpdateAsync(updateNotificationSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpDelete("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotificationSubscription(DeleteNotificationSubscriptionRequestDto deleteNotificationSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        deleteNotificationSubscriptionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await deleteNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(deleteNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        await notificationSubscriptionService.DeleteAsync(
            new NotificationSubscriptionId(deleteNotificationSubscriptionRequestDto.NotificationSubscriptionId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{notificationSubscriptionId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationSubscriptionHistory(Guid notificationSubscriptionId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.NotificationSubscription, notificationSubscriptionId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
