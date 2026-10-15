using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Accounts;
using Presentation.Mappers.Common;
using Presentation.Mappers.Dunning;
using Presentation.Models.Accounts;
using Presentation.Models.Common;
using Presentation.Models.Dunning;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[OrganizationScopeExempt(
    "Both endpoints read across every customer on the installation rather than any one tenant's record. " +
    "That is the point of them: the reconciliation screen exists because nobody knows yet which customer " +
    "a transfer belongs to, and the arrears book is read to find out who is behind.")]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("reconciliation")]
public class ReconciliationController(
    IAccountService accountService,
    IDunningService dunningService,
    IPaymentService paymentService,
    IValidator<ListUnappliedReceiptRequestDto> listUnappliedReceiptRequestValidator,
    IValidator<ListDunningStateRequestDto> listDunningStateRequestValidator) : Controller
{
    [RequirePermission(Permission.PaymentsReconcile)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("unapplied")]
    [ProducesResponseType<PagedResponseDto<AccountBalanceResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnapplied([FromQuery] ListUnappliedReceiptRequestDto listUnappliedReceiptRequestDto)
    {
        await listUnappliedReceiptRequestValidator.ValidateAndThrowAsync(
            listUnappliedReceiptRequestDto, HttpContext.RequestAborted);

        var balances = await accountService.ListUnappliedAsync(
            listUnappliedReceiptRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(balances.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.DunningRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [HttpGet("arrears")]
    [ProducesResponseType<PagedResponseDto<DunningStateResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListArrears([FromQuery] ListDunningStateRequestDto listDunningStateRequestDto)
    {
        await listDunningStateRequestValidator.ValidateAndThrowAsync(listDunningStateRequestDto, HttpContext.RequestAborted);

        var dunningStates = await dunningService.ListAsync(
            listDunningStateRequestDto.ToModel(), HttpContext.RequestAborted);

        var settlements = await paymentService.GetSettlementsAsync(
            dunningStates.Data.ConvertAll(x => x.InvoiceId), HttpContext.RequestAborted);

        var byInvoice = settlements.ToDictionary(x => (Guid)x.InvoiceId);

        return Ok(dunningStates.ToDto(x => x.ToDto(byInvoice.GetValueOrDefault(x.InvoiceId))));
    }
}
