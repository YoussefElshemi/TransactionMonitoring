using System.Net.Mime;
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
using Presentation.Mappers.Invoices;
using Presentation.Models.Common;
using Presentation.Models.Invoices;

namespace Presentation.Controllers;

[RequirePermission(Permission.InvoicesRead)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("invoices")]
public class InvoiceController(
    IApplicationUserService applicationUserService,
    IInvoiceService invoiceService,
    InvoiceResponseFactory invoiceResponseFactory,
    IValidator<InvoiceRequestDto> invoiceRequestValidator,
    IValidator<ListInvoiceRequestDto> listInvoiceRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<InvoiceResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvoices([FromQuery] ListInvoiceRequestDto listInvoiceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listInvoiceRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listInvoiceRequestValidator.ValidateAndThrowAsync(listInvoiceRequestDto, HttpContext.RequestAborted);

        var invoices = await invoiceService.ListAsync(listInvoiceRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await invoiceResponseFactory.CreateAsync(
            invoices.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(invoices.ToDto(resources));
    }

    [HttpGet("{InvoiceId:guid}")]
    [ProducesResponseType<ResponseDataDto<InvoiceResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoice(InvoiceRequestDto invoiceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        invoiceRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await invoiceRequestValidator.ValidateAndThrowAsync(invoiceRequestDto, HttpContext.RequestAborted);

        var invoice = await invoiceService.GetAsync(new InvoiceId(invoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        var resource = await invoiceResponseFactory.CreateAsync(
            invoice, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [IdempotencyExempt(
        "The response is a PDF. An idempotency record stores a JSON body, so a replay would return an empty document in place of the invoice.")]
    [HttpPost("{InvoiceId:guid}/export")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInvoice(InvoiceRequestDto invoiceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        invoiceRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await invoiceRequestValidator.ValidateAndThrowAsync(invoiceRequestDto, HttpContext.RequestAborted);

        var exportedInvoice = await invoiceService.ExportAsync(new InvoiceId(invoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        return new FileContentResult(exportedInvoice.Content, MediaTypeNames.Application.Pdf)
        {
            FileDownloadName = $"{exportedInvoice.Invoice.InvoiceId}.pdf"
        };
    }
}