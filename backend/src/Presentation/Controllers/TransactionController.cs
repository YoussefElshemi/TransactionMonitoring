using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Mappers.Parties;
using Presentation.Mappers.Transactions;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Parties;
using Presentation.Models.Transactions;

namespace Presentation.Controllers;

[RequirePermission(Permission.TransactionsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("transactions")]
public class TransactionController(
    IApplicationUserService applicationUserService,
    ITransactionService transactionService,
    IPartyService partyService,
    IAuditService auditService,
    TransactionResponseFactory transactionResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<TransactionRequestDto> transactionRequestValidator,
    IValidator<RevealTransactionRequestDto> revealTransactionRequestValidator,
    IValidator<ListTransactionRequestDto> listTransactionRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<TransactionSummaryResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactions([FromQuery] ListTransactionRequestDto listTransactionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listTransactionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listTransactionRequestValidator.ValidateAndThrowAsync(listTransactionRequestDto, HttpContext.RequestAborted);

        var transactions = await transactionService.ListAsync(listTransactionRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(transactions.ToDto(TransactionDtoMapper.ToSummaryDto));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{TransactionId:guid}")]
    [ProducesResponseType<ResponseDataDto<TransactionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransaction(TransactionRequestDto transactionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        transactionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await transactionRequestValidator.ValidateAndThrowAsync(transactionRequestDto, HttpContext.RequestAborted);

        var transaction = await transactionService.GetAsync(new TransactionId(transactionRequestDto.TransactionId), HttpContext.RequestAborted);

        var resource = await transactionResponseFactory.CreateAsync(
            transaction, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionsReveal)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{TransactionId:guid}/reveal")]
    [ProducesResponseType<ResponseDataDto<TransactionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevealTransaction(RevealTransactionRequestDto revealTransactionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        revealTransactionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await revealTransactionRequestValidator.ValidateAndThrowAsync(revealTransactionRequestDto, HttpContext.RequestAborted);

        var transaction = await transactionService.GetAsync(
            new TransactionId(revealTransactionRequestDto.TransactionId), HttpContext.RequestAborted);

        await auditService.RecordRevealAsync(new RecordRevealRequest
        {
            EntityType = AuditableEntityType.Transaction,
            EntityId = revealTransactionRequestDto.TransactionId,
            OrganizationId = authenticatedUser.OrganizationId,
            Reason = new RevealReason(revealTransactionRequestDto.Body.Reason)
        }, HttpContext.RequestAborted);

        var resource = await transactionResponseFactory.CreateRevealedAsync(
            transaction, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{TransactionId:guid}/parties")]
    [ProducesResponseType<ResponseDataDto<TransactionPartyResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactionParties(TransactionRequestDto transactionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        transactionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await transactionRequestValidator.ValidateAndThrowAsync(transactionRequestDto, HttpContext.RequestAborted);

        var transactionParties = await partyService.ListByTransactionAsync(
            new TransactionId(transactionRequestDto.TransactionId), HttpContext.RequestAborted);

        return Ok(transactionParties.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.TransactionsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{transactionId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionHistory(Guid transactionId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Transaction, transactionId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
