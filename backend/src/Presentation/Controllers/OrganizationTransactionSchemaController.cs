using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.TransactionSchemas;
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
using Presentation.Mappers.TransactionSchemas;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Controllers;

[RequirePermission(Permission.TransactionSchemasRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/transaction-schema")]
public class OrganizationTransactionSchemaController(
    IApplicationUserService applicationUserService,
    ITransactionSchemaService transactionSchemaService,
    IAuditService auditService,
    TransactionSchemaResponseFactory transactionSchemaResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<SetTransactionSchemaRequestDto> setTransactionSchemaRequestValidator,
    IValidator<TransactionSchemaRequestDto> transactionSchemaRequestValidator,
    IValidator<GetTransactionSchemaRequestDto> getTransactionSchemaRequestValidator,
    IValidator<TransactionSchemaVersionRequestDto> transactionSchemaVersionRequestValidator,
    IValidator<ListTransactionSchemaVersionRequestDto> listTransactionSchemaVersionRequestValidator,
    IValidator<ListMyTransactionSchemaVersionRequestDto> listMyTransactionSchemaVersionRequestValidator,
    IValidator<RecoverTransactionSchemaRequestDto> recoverTransactionSchemaRequestValidator,
    IValidator<TransactionSchemaHistoryRequestDto> transactionSchemaHistoryRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionSchema([FromQuery] GetTransactionSchemaRequestDto getTransactionSchemaRequestDto, Guid organizationId)
    {
        getTransactionSchemaRequestDto = getTransactionSchemaRequestDto with { OrganizationId = organizationId };

        await getTransactionSchemaRequestValidator.ValidateAndThrowAsync(getTransactionSchemaRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var transactionSchema = await ReadAsync(
            new OrganizationId(organizationId), getTransactionSchemaRequestDto.IncludeDeleted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(transactionSchema, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTransactionSchema(SetTransactionSchemaRequestDto setTransactionSchemaRequestDto)
    {
        await setTransactionSchemaRequestValidator.ValidateAndThrowAsync(setTransactionSchemaRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var transactionSchema = await transactionSchemaService.SetAsync(new SetTransactionSchemaRequest
        {
            OrganizationId = new OrganizationId(setTransactionSchemaRequestDto.OrganizationId),
            Fields = setTransactionSchemaRequestDto.Body.Fields.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(
            transactionSchema, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTransactionSchema(TransactionSchemaRequestDto transactionSchemaRequestDto)
    {
        await transactionSchemaRequestValidator.ValidateAndThrowAsync(transactionSchemaRequestDto, HttpContext.RequestAborted);

        await transactionSchemaService.DeleteAsync(new OrganizationId(transactionSchemaRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverTransactionSchema(RecoverTransactionSchemaRequestDto recoverTransactionSchemaRequestDto)
    {
        await recoverTransactionSchemaRequestValidator.ValidateAndThrowAsync(recoverTransactionSchemaRequestDto, HttpContext.RequestAborted);

        await transactionSchemaService.RecoverAsync(new OrganizationId(recoverTransactionSchemaRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("versions")]
    [ProducesResponseType<PagedResponseDto<TransactionSchemaVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactionSchemaVersions([FromQuery] ListTransactionSchemaVersionRequestDto listTransactionSchemaVersionRequestDto)
    {
        await listTransactionSchemaVersionRequestValidator.ValidateAndThrowAsync(listTransactionSchemaVersionRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var versions = await transactionSchemaService.ListAsync(listTransactionSchemaVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await transactionSchemaResponseFactory.CreateAsync(versions.Data, context, HttpContext.RequestAborted);

        return Ok(versions.ToDto(resources));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("versions/{VersionNumber:int}")]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionSchemaVersion(TransactionSchemaVersionRequestDto transactionSchemaVersionRequestDto)
    {
        await transactionSchemaVersionRequestValidator.ValidateAndThrowAsync(transactionSchemaVersionRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var version = await transactionSchemaService.GetVersionAsync(
            new OrganizationId(transactionSchemaVersionRequestDto.OrganizationId),
            new VersionNumber(transactionSchemaVersionRequestDto.VersionNumber),
            HttpContext.RequestAborted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(version, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionSchemaHistory(Guid organizationId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        await transactionSchemaHistoryRequestValidator.ValidateAndThrowAsync(
            new TransactionSchemaHistoryRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var transactionSchema = await transactionSchemaService.GetIncludingDeletedAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.TransactionSchema, transactionSchema.TransactionSchemaId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [HttpGet("/organizations/my/transaction-schema")]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactionSchema([FromQuery] bool includeDeleted)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await getTransactionSchemaRequestValidator.ValidateAndThrowAsync(new GetTransactionSchemaRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            IncludeDeleted = includeDeleted
        }, HttpContext.RequestAborted);

        var transactionSchema = await ReadAsync(new OrganizationId(authenticatedUser.OrganizationId), includeDeleted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(
            transactionSchema, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Idempotent]
    [HttpPut("/organizations/my/transaction-schema")]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMyTransactionSchema([FromBody] SetTransactionSchemaRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var setTransactionSchemaRequestDto = new SetTransactionSchemaRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await setTransactionSchemaRequestValidator.ValidateAndThrowAsync(setTransactionSchemaRequestDto, HttpContext.RequestAborted);

        var transactionSchema = await transactionSchemaService.SetAsync(new SetTransactionSchemaRequest
        {
            OrganizationId = new OrganizationId(authenticatedUser.OrganizationId),
            Fields = body.Fields.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(
            transactionSchema, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Idempotent]
    [HttpDelete("/organizations/my/transaction-schema")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyTransactionSchema()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await transactionSchemaRequestValidator.ValidateAndThrowAsync(
            new TransactionSchemaRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await transactionSchemaService.DeleteAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.TransactionSchemasWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/transaction-schema/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverMyTransactionSchema()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await recoverTransactionSchemaRequestValidator.ValidateAndThrowAsync(
            new RecoverTransactionSchemaRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await transactionSchemaService.RecoverAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("/organizations/my/transaction-schema/versions")]
    [ProducesResponseType<PagedResponseDto<TransactionSchemaVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyTransactionSchemaVersions([FromQuery] ListMyTransactionSchemaVersionRequestDto listMyTransactionSchemaVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyTransactionSchemaVersionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listMyTransactionSchemaVersionRequestValidator.ValidateAndThrowAsync(listMyTransactionSchemaVersionRequestDto, HttpContext.RequestAborted);

        var versions = await transactionSchemaService.ListAsync(listMyTransactionSchemaVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await transactionSchemaResponseFactory.CreateAsync(
            versions.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(versions.ToDto(resources));
    }

    [HttpGet("/organizations/my/transaction-schema/versions/{versionNumber:int}")]
    [ProducesResponseType<ResponseDataDto<TransactionSchemaVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactionSchemaVersion(int versionNumber)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var transactionSchemaVersionRequestDto = new TransactionSchemaVersionRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            VersionNumber = versionNumber
        };

        await transactionSchemaVersionRequestValidator.ValidateAndThrowAsync(transactionSchemaVersionRequestDto, HttpContext.RequestAborted);

        var version = await transactionSchemaService.GetVersionAsync(
            new OrganizationId(authenticatedUser.OrganizationId), new VersionNumber(versionNumber), HttpContext.RequestAborted);

        var resource = await transactionSchemaResponseFactory.CreateAsync(
            version, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.TransactionSchemasRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("/organizations/my/transaction-schema/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactionSchemaHistory([FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await transactionSchemaHistoryRequestValidator.ValidateAndThrowAsync(
            new TransactionSchemaHistoryRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        var transactionSchema = await transactionSchemaService.GetIncludingDeletedAsync(
            new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.TransactionSchema, transactionSchema.TransactionSchemaId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    private Task<TransactionSchema> ReadAsync(OrganizationId organizationId, bool includeDeleted)
    {
        return includeDeleted
            ? transactionSchemaService.GetIncludingDeletedAsync(organizationId, HttpContext.RequestAborted)
            : transactionSchemaService.GetAsync(organizationId, HttpContext.RequestAborted);
    }
}
