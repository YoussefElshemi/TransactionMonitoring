using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Parties;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Common;
using Presentation.Mappers.Parties;
using Presentation.Mappers.Transactions;
using Presentation.Models.AuditLogs;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Parties;
using Presentation.Models.Transactions;

namespace Presentation.Controllers;

[RequirePermission(Permission.PartiesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("parties")]
public class PartyController(
    IApplicationUserService applicationUserService,
    IPartyService partyService,
    IAuditService auditService,
    PartyResponseFactory partyResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<PartyRequestDto> partyRequestValidator,
    IValidator<SetPartyRiskRequestDto> setPartyRiskRequestValidator,
    IValidator<ListPartyRequestDto> listPartyRequestValidator,
    IValidator<ListPartyTransactionRequestDto> listPartyTransactionRequestValidator,
    IValidator<ListPartyCaseRequestDto> listPartyCaseRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<PartyResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListParties([FromQuery] ListPartyRequestDto listPartyRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listPartyRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listPartyRequestValidator.ValidateAndThrowAsync(listPartyRequestDto, HttpContext.RequestAborted);

        var parties = await partyService.ListAsync(listPartyRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await partyResponseFactory.CreateAsync(
            parties.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(parties.ToDto(resources));
    }

    [HttpGet("{PartyId:guid}")]
    [ProducesResponseType<ResponseDataDto<PartyResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParty(PartyRequestDto partyRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        partyRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await partyRequestValidator.ValidateAndThrowAsync(partyRequestDto, HttpContext.RequestAborted);

        var party = await partyService.GetAsync(new PartyId(partyRequestDto.PartyId), HttpContext.RequestAborted);

        var resource = await partyResponseFactory.CreateAsync(
            party, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet("{PartyId:guid}/transactions")]
    [ProducesResponseType<PagedResponseDto<TransactionSummaryResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPartyTransactions([FromQuery] ListPartyTransactionRequestDto listPartyTransactionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listPartyTransactionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listPartyTransactionRequestValidator.ValidateAndThrowAsync(listPartyTransactionRequestDto, HttpContext.RequestAborted);

        var transactions = await partyService.ListTransactionsAsync(listPartyTransactionRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(transactions.ToDto(TransactionDtoMapper.ToSummaryDto));
    }

    [RequirePermission(Permission.PartiesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut("{PartyId:guid}/risk")]
    [ProducesResponseType<ResponseDataDto<PartyResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPartyRisk(SetPartyRiskRequestDto setPartyRiskRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        setPartyRiskRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await setPartyRiskRequestValidator.ValidateAndThrowAsync(setPartyRiskRequestDto, HttpContext.RequestAborted);

        var party = await partyService.SetRiskAsync(new SetPartyRiskRequest
        {
            OrganizationId = new OrganizationId(authenticatedUser.OrganizationId),
            PartyId = new PartyId(setPartyRiskRequestDto.PartyId),
            RiskScore = setPartyRiskRequestDto.Body.RiskScore.HasValue ? new RiskScore(setPartyRiskRequestDto.Body.RiskScore.Value) : null,
            Note = new RiskNote(setPartyRiskRequestDto.Body.Note),
            SetByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await partyResponseFactory.CreateAsync(
            party, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.PartiesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{PartyId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartyHistory(Guid partyId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await partyRequestValidator.ValidateAndThrowAsync(
            new PartyRequestDto { PartyId = partyId, OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Party, partyId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [HttpGet("{PartyId:guid}/cases")]
    [ProducesResponseType<PagedResponseDto<CaseResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPartyCases([FromQuery] ListPartyCaseRequestDto listPartyCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listPartyCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listPartyCaseRequestValidator.ValidateAndThrowAsync(listPartyCaseRequestDto, HttpContext.RequestAborted);

        var cases = await partyService.ListCasesAsync(listPartyCaseRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(cases.ToDto(CaseDtoMapper.ToDto));
    }
}
