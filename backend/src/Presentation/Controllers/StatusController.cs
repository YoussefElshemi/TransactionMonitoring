using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Mappers.Common;
using Presentation.Mappers.Status;
using Presentation.Models.Common;
using Presentation.Models.Status;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[AllowAnonymous]
[Route("status")]
public class StatusController(
    IStatusService statusService,
    IIncidentService incidentService,
    IStatusSubscriptionService statusSubscriptionService,
    IValidator<ListIncidentRequestDto> listIncidentRequestValidator,
    IValidator<CreateStatusSubscriptionRequestDto> createStatusSubscriptionRequestValidator,
    IValidator<ConfirmStatusSubscriptionRequestDto> confirmStatusSubscriptionRequestValidator,
    IValidator<UnsubscribeStatusSubscriptionRequestDto> unsubscribeStatusSubscriptionRequestValidator) : Controller
{
    [RateLimitExempt]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<ServiceStatusResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var serviceStatus = await statusService.GetAsync(HttpContext.RequestAborted);

        return Ok(serviceStatus.ToDto().ToResponseData());
    }

    [RateLimitExempt]
    [HttpGet("heartbeat")]
    [ProducesResponseType<ResponseDataDto<ServiceHeartbeatResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeartbeat()
    {
        var heartbeat = await statusService.GetHeartbeatAsync(HttpContext.RequestAborted);

        return Ok(heartbeat.ToDto().ToResponseData());
    }

    [RateLimitExempt]
    [HttpGet("incidents")]
    [ProducesResponseType<PagedResponseDto<IncidentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIncidents([FromQuery] ListIncidentRequestDto listIncidentRequestDto)
    {
        await listIncidentRequestValidator.ValidateAndThrowAsync(listIncidentRequestDto, HttpContext.RequestAborted);

        var incidents = await incidentService.ListAsync(listIncidentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(incidents.ToDto(x => x.ToDto()));
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. Subscribing twice reissues a confirmation rather than creating a second subscription.")]
    [RateLimitExempt]
    [HttpPost("subscriptions")]
    [ProducesResponseType<ResponseDataDto<StatusSubscriptionResponseDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Subscribe(CreateStatusSubscriptionRequestDto createStatusSubscriptionRequestDto)
    {
        await createStatusSubscriptionRequestValidator.ValidateAndThrowAsync(createStatusSubscriptionRequestDto, HttpContext.RequestAborted);

        var issued = await statusSubscriptionService.SubscribeAsync(
            createStatusSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status202Accepted, issued.ToDto().ToResponseData());
    }

    [IdempotencyExempt("Anonymous, and idempotent by nature: confirming an already confirmed subscription changes nothing.")]
    [RateLimitExempt]
    [HttpGet("subscriptions/{StatusSubscriptionId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmSubscription(ConfirmStatusSubscriptionRequestDto confirmStatusSubscriptionRequestDto)
    {
        await confirmStatusSubscriptionRequestValidator.ValidateAndThrowAsync(confirmStatusSubscriptionRequestDto, HttpContext.RequestAborted);

        await statusSubscriptionService.ConfirmAsync(
            confirmStatusSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [IdempotencyExempt("Anonymous, and idempotent by nature: unsubscribing twice leaves the same row in the same state.")]
    [RateLimitExempt]
    [HttpGet("subscriptions/{StatusSubscriptionId:guid}/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unsubscribe(UnsubscribeStatusSubscriptionRequestDto unsubscribeStatusSubscriptionRequestDto)
    {
        await unsubscribeStatusSubscriptionRequestValidator.ValidateAndThrowAsync(unsubscribeStatusSubscriptionRequestDto, HttpContext.RequestAborted);

        await statusSubscriptionService.UnsubscribeAsync(
            unsubscribeStatusSubscriptionRequestDto.ToUnsubscribeModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
