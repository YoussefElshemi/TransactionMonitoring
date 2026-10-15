using System.Net.Mime;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Payments;
using Presentation.Models.Common;
using Presentation.Models.Payments;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/payments")]
public class OrganizationPaymentController(
    IApplicationUserService applicationUserService,
    IPaymentService paymentService,
    IValidator<ListPaymentRequestDto> listPaymentRequestValidator,
    IValidator<PaymentRequestDto> paymentRequestValidator,
    IValidator<RecordPaymentRequestDto> recordPaymentRequestValidator,
    IValidator<CollectPaymentRequestDto> collectPaymentRequestValidator) : Controller
{
    [RequirePermission(Permission.PaymentsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<PaymentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPayments([FromQuery] ListPaymentRequestDto listPaymentRequestDto)
    {
        await listPaymentRequestValidator.ValidateAndThrowAsync(listPaymentRequestDto, HttpContext.RequestAborted);

        var payments = await paymentService.ListAsync(listPaymentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(payments.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.PaymentsRead)]
    [HttpGet("{PaymentId:guid}")]
    [ProducesResponseType<ResponseDataDto<PaymentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayment(PaymentRequestDto paymentRequestDto)
    {
        await paymentRequestValidator.ValidateAndThrowAsync(paymentRequestDto, HttpContext.RequestAborted);

        var payment = await paymentService.GetAsync(new PaymentId(paymentRequestDto.PaymentId), HttpContext.RequestAborted);

        return Ok(payment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PaymentsRead)]
    [IdempotencyExempt(
        "The response is a PDF. An idempotency record stores a JSON body, so a replay would return an empty document in place of the receipt.")]
    [HttpPost("{PaymentId:guid}/receipt")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReceipt(PaymentRequestDto paymentRequestDto)
    {
        await paymentRequestValidator.ValidateAndThrowAsync(paymentRequestDto, HttpContext.RequestAborted);

        var exportedReceipt = await paymentService.ExportReceiptAsync(
            new PaymentId(paymentRequestDto.PaymentId), HttpContext.RequestAborted);

        return new FileContentResult(exportedReceipt.Content, MediaTypeNames.Application.Pdf)
        {
            FileDownloadName = $"{exportedReceipt.Payment.ReceiptNumber}.pdf"
        };
    }

    [RequirePermission(Permission.PaymentsReconcile)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<PaymentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordPayment(RecordPaymentRequestDto recordPaymentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recordPaymentRequestDto.RecordedByUserId = authenticatedUser.UserId;
        await recordPaymentRequestValidator.ValidateAndThrowAsync(recordPaymentRequestDto, HttpContext.RequestAborted);

        var payment = await paymentService.RecordAsync(recordPaymentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, payment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PaymentsCollect)]
    [Idempotent]
    [HttpPost("/organizations/{OrganizationId:guid}/invoices/{InvoiceId:guid}/collect")]
    [ProducesResponseType<ResponseDataDto<PaymentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CollectPayment(CollectPaymentRequestDto collectPaymentRequestDto)
    {
        await collectPaymentRequestValidator.ValidateAndThrowAsync(collectPaymentRequestDto, HttpContext.RequestAborted);

        var payment = await paymentService.CollectAsync(collectPaymentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, payment.ToDto().ToResponseData());
    }
}
