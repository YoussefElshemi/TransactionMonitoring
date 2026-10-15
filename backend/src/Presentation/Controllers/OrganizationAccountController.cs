using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Accounts;
using Presentation.Mappers.Common;
using Presentation.Models.Accounts;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/account")]
public class OrganizationAccountController(
    IApplicationUserService applicationUserService,
    IAccountService accountService,
    IValidator<AccountRequestDto> accountRequestValidator,
    IValidator<ListAccountEntryRequestDto> listAccountEntryRequestValidator,
    IValidator<AllocatePaymentRequestDto> allocatePaymentRequestValidator,
    IValidator<RefundBalanceRequestDto> refundBalanceRequestValidator) : Controller
{
    [RequirePermission(Permission.PaymentsRead)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<AccountBalanceResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances(AccountRequestDto accountRequestDto)
    {
        await accountRequestValidator.ValidateAndThrowAsync(accountRequestDto, HttpContext.RequestAborted);

        var balances = await accountService.GetBalancesAsync(
            new OrganizationId(accountRequestDto.OrganizationId), HttpContext.RequestAborted);

        return Ok(balances.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.PaymentsRead)]
    [HttpGet("entries")]
    [ProducesResponseType<PagedResponseDto<AccountEntryResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAccountEntries([FromQuery] ListAccountEntryRequestDto listAccountEntryRequestDto)
    {
        await listAccountEntryRequestValidator.ValidateAndThrowAsync(listAccountEntryRequestDto, HttpContext.RequestAborted);

        var accountEntries = await accountService.ListAsync(
            listAccountEntryRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(accountEntries.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.PaymentsReconcile)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("allocations")]
    [ProducesResponseType<ResponseDataDto<AccountEntryResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AllocatePayment(AllocatePaymentRequestDto allocatePaymentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        allocatePaymentRequestDto.AllocatedByUserId = authenticatedUser.UserId;
        await allocatePaymentRequestValidator.ValidateAndThrowAsync(allocatePaymentRequestDto, HttpContext.RequestAborted);

        var accountEntry = await accountService.AllocateAsync(
            allocatePaymentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, accountEntry.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PaymentsRefund)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("refunds")]
    [ProducesResponseType<ResponseDataDto<AccountEntryResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> RefundBalance(RefundBalanceRequestDto refundBalanceRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        refundBalanceRequestDto.RefundedByUserId = authenticatedUser.UserId;
        await refundBalanceRequestValidator.ValidateAndThrowAsync(refundBalanceRequestDto, HttpContext.RequestAborted);

        var accountEntry = await accountService.RefundAsync(
            refundBalanceRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, accountEntry.ToDto().ToResponseData());
    }
}
