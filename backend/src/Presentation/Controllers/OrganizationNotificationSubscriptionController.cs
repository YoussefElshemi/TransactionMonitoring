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
using Presentation.Mappers.NotificationSubscriptions;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Controllers;

[RequirePermission(Permission.NotificationSubscriptionsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/notification-subscriptions")]
public class OrganizationNotificationSubscriptionController(
    IApplicationUserService applicationUserService,
    INotificationSubscriptionService notificationSubscriptionService,
    IAuditService auditService,
    NotificationSubscriptionResponseFactory notificationSubscriptionResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationListNotificationSubscriptionRequestDto> organizationListNotificationSubscriptionRequestValidator,
    IValidator<OrganizationNotificationSubscriptionRequestDto> organizationNotificationSubscriptionRequestValidator,
    IValidator<OrganizationCreateNotificationSubscriptionRequestDto> organizationCreateNotificationSubscriptionRequestValidator,
    IValidator<OrganizationUpdateNotificationSubscriptionRequestDto> organizationUpdateNotificationSubscriptionRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotificationSubscriptions(
        [FromQuery] OrganizationListNotificationSubscriptionRequestDto organizationListNotificationSubscriptionRequestDto)
    {
        await organizationListNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(
            organizationListNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var notificationSubscriptions = await notificationSubscriptionService.ListAsync(
            organizationListNotificationSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = notificationSubscriptionResponseFactory.Create(notificationSubscriptions.Data);

        return Ok(notificationSubscriptions.ToDto(resources));
    }

    [HttpGet("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType<ResponseDataDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationSubscription(
        OrganizationNotificationSubscriptionRequestDto organizationNotificationSubscriptionRequestDto)
    {
        await organizationNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(
            organizationNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var notificationSubscription = await notificationSubscriptionService.GetAsync(
            new NotificationSubscriptionId(organizationNotificationSubscriptionRequestDto.NotificationSubscriptionId),
            HttpContext.RequestAborted);

        var resource = notificationSubscriptionResponseFactory.Create(notificationSubscription);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<NotificationSubscriptionResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNotificationSubscription(
        OrganizationCreateNotificationSubscriptionRequestDto organizationCreateNotificationSubscriptionRequestDto)
    {
        await organizationCreateNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(
            organizationCreateNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        var notificationSubscription = await notificationSubscriptionService.CreateAsync(
            organizationCreateNotificationSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resource = notificationSubscriptionResponseFactory.Create(notificationSubscription);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpPatch("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationSubscription(
        OrganizationUpdateNotificationSubscriptionRequestDto organizationUpdateNotificationSubscriptionRequestDto)
    {
        await organizationUpdateNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(
            organizationUpdateNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        await notificationSubscriptionService.UpdateAsync(
            organizationUpdateNotificationSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.NotificationSubscriptionsWrite)]
    [Idempotent]
    [HttpDelete("{NotificationSubscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNotificationSubscription(
        OrganizationNotificationSubscriptionRequestDto organizationNotificationSubscriptionRequestDto)
    {
        await organizationNotificationSubscriptionRequestValidator.ValidateAndThrowAsync(
            organizationNotificationSubscriptionRequestDto, HttpContext.RequestAborted);

        await notificationSubscriptionService.DeleteAsync(
            new NotificationSubscriptionId(organizationNotificationSubscriptionRequestDto.NotificationSubscriptionId),
            HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{notificationSubscriptionId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationSubscriptionHistory(
        Guid organizationId,
        Guid notificationSubscriptionId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.NotificationSubscription, notificationSubscriptionId),
            HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
