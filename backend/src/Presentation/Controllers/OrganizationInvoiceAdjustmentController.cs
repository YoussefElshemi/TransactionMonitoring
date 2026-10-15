using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Adjustments;
using Presentation.Mappers.Common;
using Presentation.Models.Adjustments;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/invoice-adjustments")]
public class OrganizationInvoiceAdjustmentController(
    IApplicationUserService applicationUserService,
    IInvoiceAdjustmentService invoiceAdjustmentService,
    IValidator<CreateInvoiceAdjustmentRequestDto> createInvoiceAdjustmentRequestValidator,
    IValidator<InvoiceAdjustmentRequestDto> invoiceAdjustmentRequestValidator,
    IValidator<DeleteInvoiceAdjustmentRequestDto> deleteInvoiceAdjustmentRequestValidator,
    IValidator<ListInvoiceAdjustmentRequestDto> listInvoiceAdjustmentRequestValidator) : Controller
{
    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<InvoiceAdjustmentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvoiceAdjustment(CreateInvoiceAdjustmentRequestDto createInvoiceAdjustmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createInvoiceAdjustmentRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createInvoiceAdjustmentRequestValidator.ValidateAndThrowAsync(createInvoiceAdjustmentRequestDto, HttpContext.RequestAborted);

        var invoiceAdjustment = await invoiceAdjustmentService.CreateAsync(
            createInvoiceAdjustmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, invoiceAdjustment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<InvoiceAdjustmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvoiceAdjustments([FromQuery] ListInvoiceAdjustmentRequestDto listInvoiceAdjustmentRequestDto)
    {
        await listInvoiceAdjustmentRequestValidator.ValidateAndThrowAsync(listInvoiceAdjustmentRequestDto, HttpContext.RequestAborted);

        var invoiceAdjustments = await invoiceAdjustmentService.ListAsync(
            listInvoiceAdjustmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(invoiceAdjustments.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet("{InvoiceAdjustmentId:guid}")]
    [ProducesResponseType<ResponseDataDto<InvoiceAdjustmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoiceAdjustment(InvoiceAdjustmentRequestDto invoiceAdjustmentRequestDto)
    {
        await invoiceAdjustmentRequestValidator.ValidateAndThrowAsync(invoiceAdjustmentRequestDto, HttpContext.RequestAborted);

        var invoiceAdjustment = await invoiceAdjustmentService.GetAsync(
            new InvoiceAdjustmentId(invoiceAdjustmentRequestDto.InvoiceAdjustmentId), HttpContext.RequestAborted);

        return Ok(invoiceAdjustment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{InvoiceAdjustmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteInvoiceAdjustment(DeleteInvoiceAdjustmentRequestDto deleteInvoiceAdjustmentRequestDto)
    {
        await deleteInvoiceAdjustmentRequestValidator.ValidateAndThrowAsync(deleteInvoiceAdjustmentRequestDto, HttpContext.RequestAborted);

        await invoiceAdjustmentService.DeleteAsync(
            new InvoiceAdjustmentId(deleteInvoiceAdjustmentRequestDto.InvoiceAdjustmentId), HttpContext.RequestAborted);

        return NoContent();
    }
}
