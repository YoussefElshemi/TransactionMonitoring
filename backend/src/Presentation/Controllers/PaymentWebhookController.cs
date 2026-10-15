using Core.Factories;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using ILogger = Serilog.ILogger;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[RateLimitExempt]
[ApiController]
[AllowAnonymous]
[Route("payments/webhooks")]
public class PaymentWebhookController(
    IPaymentProvider paymentProvider,
    IPaymentService paymentService,
    IProviderEventRepository providerEventRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger logger) : Controller
{
    [IdempotencyExempt(
        "The caller is the provider rather than a client of ours, and it presents no idempotency key. " +
        "Redelivery is handled where it has to be — the event is claimed by a unique insert on the " +
        "provider's own event id before anything acts on it, so the second delivery loses the race.")]
    [HttpPost("provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveProviderEvent()
    {
        // Read the raw bytes before anything parses them: the signature is over these exactly.
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(HttpContext.RequestAborted);

        var providerEvent = paymentProvider.ReadEvent(payload, Request.Headers["Stripe-Signature"]);

        if (providerEvent is null)
        {
            return BadRequest();
        }

        var providerEventRecord = ProviderEventRecordFactory.Create(providerEvent, timeProvider);

        // Inserted under a unique reference before acting, so a redelivery loses the insert and is not applied twice.
        if (!await providerEventRepository.TryAddAsync(providerEventRecord, HttpContext.RequestAborted))
        {
            return Ok();
        }

        try
        {
            var paymentId = await paymentService.HandleEventAsync(providerEvent, HttpContext.RequestAborted);

            await providerEventRepository.ProcessAsync(
                providerEventRecord.ProviderEventRecordId, paymentId, HttpContext.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(
                exception,
                "Handling provider event {EventReference} ({EventType}) failed: {Message}",
                providerEvent.EventReference.ToString(),
                providerEvent.EventType.ToString(),
                exception.Message);

            await providerEventRepository.FailAsync(
                providerEventRecord.ProviderEventRecordId,
                new FailureReason(exception.Message),
                HttpContext.RequestAborted);
        }

        await unitOfWork.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok();
    }
}
