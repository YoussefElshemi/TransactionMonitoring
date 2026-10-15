using System.Net.Mime;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Invoices;
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
using Presentation.Models.OrganizationInvoices;

namespace Presentation.Controllers;

[RequirePermission(Permission.InvoicesRead)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("organizations/{OrganizationId:guid}/invoices")]
public class OrganizationInvoiceController(
    IApplicationUserService applicationUserService,
    IInvoiceService invoiceService,
    InvoiceResponseFactory invoiceResponseFactory,
    IValidator<GenerateOrganizationInvoiceRequestDto> generateOrganizationInvoiceRequestValidator,
    IValidator<IssueOrganizationInvoiceRequestDto> issueOrganizationInvoiceRequestValidator,
    IValidator<OrganizationInvoiceRequestDto> organizationInvoiceRequestValidator,
    IValidator<RecoverOrganizationInvoiceRequestDto> recoverOrganizationInvoiceRequestValidator,
    IValidator<ListOrganizationInvoiceRequestDto> listOrganizationInvoiceRequestValidator) : Controller
{
    [RequirePermission(Permission.InvoicesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<InvoiceResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateInvoice(GenerateOrganizationInvoiceRequestDto generateOrganizationInvoiceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await generateOrganizationInvoiceRequestValidator.ValidateAndThrowAsync(generateOrganizationInvoiceRequestDto, HttpContext.RequestAborted);

        var generateInvoiceRequest = new GenerateInvoiceRequest
        {
            OrganizationId = new OrganizationId(generateOrganizationInvoiceRequestDto.OrganizationId),
            Month = new Month(generateOrganizationInvoiceRequestDto.Body.Month),
            Year = new Year(generateOrganizationInvoiceRequestDto.Body.Year),
            CreatedByUserId = authenticatedUser.UserId
        };

        var invoice = await invoiceService.GenerateAsync(generateInvoiceRequest, HttpContext.RequestAborted);

        var resource = await invoiceResponseFactory.CreateAsync(
            invoice, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.InvoicesWrite)]
    [Idempotent]
    [HttpPost("{InvoiceId:guid}/issue")]
    [ProducesResponseType<ResponseDataDto<InvoiceResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueInvoice(IssueOrganizationInvoiceRequestDto issueOrganizationInvoiceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await issueOrganizationInvoiceRequestValidator.ValidateAndThrowAsync(issueOrganizationInvoiceRequestDto, HttpContext.RequestAborted);

        var invoice = await invoiceService.IssueAsync(
            new InvoiceId(issueOrganizationInvoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        var resource = await invoiceResponseFactory.CreateAsync(
            invoice, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<InvoiceResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvoices([FromQuery] ListOrganizationInvoiceRequestDto listOrganizationInvoiceRequestDto)
    {
        await listOrganizationInvoiceRequestValidator.ValidateAndThrowAsync(listOrganizationInvoiceRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var invoices = await invoiceService.ListAsync(listOrganizationInvoiceRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await invoiceResponseFactory.CreateAsync(invoices.Data, context, HttpContext.RequestAborted);

        return Ok(invoices.ToDto(resources));
    }

    [HttpGet("{InvoiceId:guid}")]
    [ProducesResponseType<ResponseDataDto<InvoiceResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoice(OrganizationInvoiceRequestDto invoiceRequestDto)
    {
        await organizationInvoiceRequestValidator.ValidateAndThrowAsync(invoiceRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var invoice = await invoiceService.GetAsync(new InvoiceId(invoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        var resource = await invoiceResponseFactory.CreateAsync(invoice, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [IdempotencyExempt(
        "The response is a PDF. An idempotency record stores a JSON body, so a replay would return an empty document in place of the invoice.")]
    [HttpPost("{InvoiceId:guid}/export")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInvoice(OrganizationInvoiceRequestDto invoiceRequestDto)
    {
        await organizationInvoiceRequestValidator.ValidateAndThrowAsync(invoiceRequestDto, HttpContext.RequestAborted);

        var exportedInvoice = await invoiceService.ExportAsync(new InvoiceId(invoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        return new FileContentResult(exportedInvoice.Content, MediaTypeNames.Application.Pdf)
        {
            FileDownloadName = $"{exportedInvoice.Invoice.InvoiceId}.pdf"
        };
    }

    [RequirePermission(Permission.InvoicesWrite)]
    [Idempotent]
    [HttpDelete("{InvoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteInvoice(OrganizationInvoiceRequestDto invoiceRequestDto)
    {
        await organizationInvoiceRequestValidator.ValidateAndThrowAsync(invoiceRequestDto, HttpContext.RequestAborted);

        await invoiceService.DeleteAsync(new InvoiceId(invoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.InvoicesWrite)]
    [Idempotent]
    [HttpPost("{InvoiceId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverInvoice(RecoverOrganizationInvoiceRequestDto recoverOrganizationInvoiceRequestDto)
    {
        await recoverOrganizationInvoiceRequestValidator.ValidateAndThrowAsync(recoverOrganizationInvoiceRequestDto, HttpContext.RequestAborted);

        await invoiceService.RecoverAsync(new InvoiceId(recoverOrganizationInvoiceRequestDto.InvoiceId), HttpContext.RequestAborted);

        return NoContent();
    }
}