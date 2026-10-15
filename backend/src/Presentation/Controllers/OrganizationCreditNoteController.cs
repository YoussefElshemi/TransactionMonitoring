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
using Presentation.Mappers.CreditNotes;
using Presentation.Models.Common;
using Presentation.Models.CreditNotes;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}")]
public class OrganizationCreditNoteController(
    IApplicationUserService applicationUserService,
    ICreditNoteService creditNoteService,
    IValidator<CreateCreditNoteRequestDto> createCreditNoteRequestValidator,
    IValidator<CreditNoteRequestDto> creditNoteRequestValidator,
    IValidator<ExportCreditNoteRequestDto> exportCreditNoteRequestValidator,
    IValidator<ListCreditNoteRequestDto> listCreditNoteRequestValidator) : Controller
{
    [RequirePermission(Permission.CreditNotesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("invoices/{InvoiceId:guid}/credit-notes")]
    [ProducesResponseType<ResponseDataDto<CreditNoteResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCreditNote(CreateCreditNoteRequestDto createCreditNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createCreditNoteRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createCreditNoteRequestValidator.ValidateAndThrowAsync(createCreditNoteRequestDto, HttpContext.RequestAborted);

        var creditNote = await creditNoteService.CreateAsync(
            createCreditNoteRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, creditNote.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.CreditNotesRead)]
    [HttpGet("credit-notes")]
    [ProducesResponseType<PagedResponseDto<CreditNoteResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCreditNotes([FromQuery] ListCreditNoteRequestDto listCreditNoteRequestDto)
    {
        await listCreditNoteRequestValidator.ValidateAndThrowAsync(listCreditNoteRequestDto, HttpContext.RequestAborted);

        var creditNotes = await creditNoteService.ListAsync(
            listCreditNoteRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(creditNotes.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.CreditNotesRead)]
    [HttpGet("credit-notes/{CreditNoteId:guid}")]
    [ProducesResponseType<ResponseDataDto<CreditNoteResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreditNote(CreditNoteRequestDto creditNoteRequestDto)
    {
        await creditNoteRequestValidator.ValidateAndThrowAsync(creditNoteRequestDto, HttpContext.RequestAborted);

        var creditNote = await creditNoteService.GetAsync(
            new CreditNoteId(creditNoteRequestDto.CreditNoteId), HttpContext.RequestAborted);

        return Ok(creditNote.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.CreditNotesRead)]
    [IdempotencyExempt(
        "The response is a PDF. An idempotency record stores a JSON body, so a replay would return an empty document in place of the credit note.")]
    [HttpPost("credit-notes/{CreditNoteId:guid}/export")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCreditNote(ExportCreditNoteRequestDto exportCreditNoteRequestDto)
    {
        await exportCreditNoteRequestValidator.ValidateAndThrowAsync(exportCreditNoteRequestDto, HttpContext.RequestAborted);

        var exportedCreditNote = await creditNoteService.ExportAsync(
            new CreditNoteId(exportCreditNoteRequestDto.CreditNoteId), HttpContext.RequestAborted);

        return new FileContentResult(exportedCreditNote.Content, MediaTypeNames.Application.Pdf)
        {
            FileDownloadName = $"{exportedCreditNote.CreditNote.CreditNoteNumber}.pdf"
        };
    }
}
