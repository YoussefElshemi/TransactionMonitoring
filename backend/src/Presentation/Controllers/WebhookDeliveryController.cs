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
using Presentation.Mappers.Common;
using Presentation.Mappers.Webhooks;
using Presentation.Models.Common;
using Presentation.Models.Webhooks;

namespace Presentation.Controllers;

[RequirePermission(Permission.WebhookConfigurationsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/webhook-deliveries")]
public class WebhookDeliveryController(
    IApplicationUserService applicationUserService,
    IWebhookDeliveryService webhookDeliveryService,
    IWebhookCatalogueService webhookCatalogueService,
    WebhookDeliveryResponseFactory webhookDeliveryResponseFactory,
    IValidator<ListWebhookDeliveryRequestDto> listWebhookDeliveryRequestValidator,
    IValidator<ListMyWebhookDeliveryRequestDto> listMyWebhookDeliveryRequestValidator,
    IValidator<ReplayWebhookDeliveryRequestDto> replayWebhookDeliveryRequestValidator,
    IValidator<ReplayWebhookDeliveryRangeRequestDto> replayWebhookDeliveryRangeRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<WebhookDeliveryResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWebhookDeliveries([FromQuery] ListWebhookDeliveryRequestDto listWebhookDeliveryRequestDto)
    {
        await listWebhookDeliveryRequestValidator.ValidateAndThrowAsync(listWebhookDeliveryRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var deliveries = await webhookDeliveryService.ListAsync(listWebhookDeliveryRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookDeliveryResponseFactory.CreateAsync(deliveries.Data, context, HttpContext.RequestAborted);

        return Ok(deliveries.ToDto(resources));
    }

    [HttpGet("{outboxMessageId:guid}")]
    [ProducesResponseType<ResponseDataDto<WebhookDeliveryResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookDelivery(Guid organizationId, Guid outboxMessageId)
    {
        await replayWebhookDeliveryRequestValidator.ValidateAndThrowAsync(
            new ReplayWebhookDeliveryRequestDto { OrganizationId = organizationId, OutboxMessageId = outboxMessageId },
            HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var delivery = await webhookDeliveryService.GetAsync(new OutboxMessageId(outboxMessageId), HttpContext.RequestAborted);

        var resource = await webhookDeliveryResponseFactory.CreateAsync(delivery, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("{outboxMessageId:guid}/replay")]
    [ProducesResponseType<ResponseDataDto<WebhookDeliveryResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReplayWebhookDelivery(Guid organizationId, Guid outboxMessageId)
    {
        await replayWebhookDeliveryRequestValidator.ValidateAndThrowAsync(
            new ReplayWebhookDeliveryRequestDto { OrganizationId = organizationId, OutboxMessageId = outboxMessageId },
            HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var replay = await webhookDeliveryService.ReplayAsync(new OutboxMessageId(outboxMessageId), HttpContext.RequestAborted);

        var resource = await webhookDeliveryResponseFactory.CreateAsync(replay, context, HttpContext.RequestAborted);

        return Accepted(resource.ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("replay")]
    [ProducesResponseType<ResponseDataDto<WebhookReplayBatchResponseDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReplayWebhookDeliveryRange(ReplayWebhookDeliveryRangeRequestDto replayWebhookDeliveryRangeRequestDto)
    {
        await replayWebhookDeliveryRangeRequestValidator.ValidateAndThrowAsync(replayWebhookDeliveryRangeRequestDto, HttpContext.RequestAborted);

        var batch = await webhookDeliveryService.ReplayRangeAsync(replayWebhookDeliveryRangeRequestDto.ToModel(), HttpContext.RequestAborted);

        return Accepted(batch.ToDto().ToResponseData());
    }

    [HttpGet("/organizations/my/webhook-deliveries")]
    [ProducesResponseType<PagedResponseDto<WebhookDeliveryResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyWebhookDeliveries([FromQuery] ListMyWebhookDeliveryRequestDto listMyWebhookDeliveryRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyWebhookDeliveryRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await listMyWebhookDeliveryRequestValidator.ValidateAndThrowAsync(listMyWebhookDeliveryRequestDto, HttpContext.RequestAborted);

        var deliveries = await webhookDeliveryService.ListAsync(listMyWebhookDeliveryRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookDeliveryResponseFactory.CreateAsync(
            deliveries.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(deliveries.ToDto(resources));
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-deliveries/{outboxMessageId:guid}/replay")]
    [ProducesResponseType<ResponseDataDto<WebhookDeliveryResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReplayMyWebhookDelivery(Guid outboxMessageId)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await replayWebhookDeliveryRequestValidator.ValidateAndThrowAsync(
            new ReplayWebhookDeliveryRequestDto { OrganizationId = authenticatedUser.OrganizationId, OutboxMessageId = outboxMessageId },
            HttpContext.RequestAborted);

        var replay = await webhookDeliveryService.ReplayAsync(new OutboxMessageId(outboxMessageId), HttpContext.RequestAborted);

        var resource = await webhookDeliveryResponseFactory.CreateAsync(
            replay, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Accepted(resource.ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-deliveries/replay")]
    [ProducesResponseType<ResponseDataDto<WebhookReplayBatchResponseDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReplayMyWebhookDeliveryRange([FromBody] ReplayWebhookDeliveryRangeRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var replayWebhookDeliveryRangeRequestDto = new ReplayWebhookDeliveryRangeRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await replayWebhookDeliveryRangeRequestValidator.ValidateAndThrowAsync(replayWebhookDeliveryRangeRequestDto, HttpContext.RequestAborted);

        var batch = await webhookDeliveryService.ReplayRangeAsync(replayWebhookDeliveryRangeRequestDto.ToModel(), HttpContext.RequestAborted);

        return Accepted(batch.ToDto().ToResponseData());
    }

    [HttpGet("/webhooks/catalogue")]
    [ProducesResponseType<ResponseDataDto<WebhookCatalogueResponseDto>>(StatusCodes.Status200OK)]
    public IActionResult GetWebhookCatalogue()
    {
        return Ok(webhookCatalogueService.Get().ToDto().ToResponseData());
    }
}
