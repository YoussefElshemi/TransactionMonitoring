using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.PartyMappings;
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
using Presentation.Mappers.PartyMappings;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.PartyMappings;

namespace Presentation.Controllers;

[RequirePermission(Permission.PartyMappingsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/party-mapping")]
public class OrganizationPartyMappingController(
    IApplicationUserService applicationUserService,
    IPartyMappingService partyMappingService,
    IAuditService auditService,
    PartyMappingResponseFactory partyMappingResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<SetPartyMappingRequestDto> setPartyMappingRequestValidator,
    IValidator<PartyMappingRequestDto> partyMappingRequestValidator,
    IValidator<GetPartyMappingRequestDto> getPartyMappingRequestValidator,
    IValidator<PartyMappingVersionRequestDto> partyMappingVersionRequestValidator,
    IValidator<ListPartyMappingVersionRequestDto> listPartyMappingVersionRequestValidator,
    IValidator<ListMyPartyMappingVersionRequestDto> listMyPartyMappingVersionRequestValidator,
    IValidator<RecoverPartyMappingRequestDto> recoverPartyMappingRequestValidator,
    IValidator<PartyMappingHistoryRequestDto> partyMappingHistoryRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<PartyMappingResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartyMapping([FromQuery] GetPartyMappingRequestDto getPartyMappingRequestDto, Guid organizationId)
    {
        getPartyMappingRequestDto = getPartyMappingRequestDto with { OrganizationId = organizationId };

        await getPartyMappingRequestValidator.ValidateAndThrowAsync(getPartyMappingRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var partyMapping = await ReadAsync(new OrganizationId(organizationId), getPartyMappingRequestDto.IncludeDeleted);

        var resource = await partyMappingResponseFactory.CreateAsync(partyMapping, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType<ResponseDataDto<PartyMappingResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPartyMapping(SetPartyMappingRequestDto setPartyMappingRequestDto)
    {
        await setPartyMappingRequestValidator.ValidateAndThrowAsync(setPartyMappingRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var partyMapping = await partyMappingService.SetAsync(new SetPartyMappingRequest
        {
            OrganizationId = new OrganizationId(setPartyMappingRequestDto.OrganizationId),
            Mappings = setPartyMappingRequestDto.Body.Mappings.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await partyMappingResponseFactory.CreateAsync(
            partyMapping, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePartyMapping(PartyMappingRequestDto partyMappingRequestDto)
    {
        await partyMappingRequestValidator.ValidateAndThrowAsync(partyMappingRequestDto, HttpContext.RequestAborted);

        await partyMappingService.DeleteAsync(new OrganizationId(partyMappingRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverPartyMapping(RecoverPartyMappingRequestDto recoverPartyMappingRequestDto)
    {
        await recoverPartyMappingRequestValidator.ValidateAndThrowAsync(recoverPartyMappingRequestDto, HttpContext.RequestAborted);

        await partyMappingService.RecoverAsync(new OrganizationId(recoverPartyMappingRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("versions")]
    [ProducesResponseType<PagedResponseDto<PartyMappingVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPartyMappingVersions([FromQuery] ListPartyMappingVersionRequestDto listPartyMappingVersionRequestDto)
    {
        await listPartyMappingVersionRequestValidator.ValidateAndThrowAsync(listPartyMappingVersionRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var versions = await partyMappingService.ListAsync(listPartyMappingVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await partyMappingResponseFactory.CreateAsync(versions.Data, context, HttpContext.RequestAborted);

        return Ok(versions.ToDto(resources));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("versions/{VersionNumber:int}")]
    [ProducesResponseType<ResponseDataDto<PartyMappingVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartyMappingVersion(PartyMappingVersionRequestDto partyMappingVersionRequestDto)
    {
        await partyMappingVersionRequestValidator.ValidateAndThrowAsync(partyMappingVersionRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var version = await partyMappingService.GetVersionAsync(
            new OrganizationId(partyMappingVersionRequestDto.OrganizationId),
            new VersionNumber(partyMappingVersionRequestDto.VersionNumber),
            HttpContext.RequestAborted);

        var resource = await partyMappingResponseFactory.CreateAsync(version, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("usages")]
    [ProducesResponseType<ResponseDataDto<PartyMappingUsageResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPartyMappingUsages(Guid organizationId)
    {
        await partyMappingHistoryRequestValidator.ValidateAndThrowAsync(
            new PartyMappingHistoryRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var usages = await partyMappingService.ListUsagesAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        return Ok(usages.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartyMappingHistory(Guid organizationId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        await partyMappingHistoryRequestValidator.ValidateAndThrowAsync(
            new PartyMappingHistoryRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var partyMapping = await partyMappingService.GetIncludingDeletedAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.PartyMapping, partyMapping.PartyMappingId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [HttpGet("/organizations/my/party-mapping")]
    [ProducesResponseType<ResponseDataDto<PartyMappingResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPartyMapping([FromQuery] bool includeDeleted)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await getPartyMappingRequestValidator.ValidateAndThrowAsync(new GetPartyMappingRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            IncludeDeleted = includeDeleted
        }, HttpContext.RequestAborted);

        var partyMapping = await ReadAsync(new OrganizationId(authenticatedUser.OrganizationId), includeDeleted);

        var resource = await partyMappingResponseFactory.CreateAsync(
            partyMapping, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Idempotent]
    [HttpPut("/organizations/my/party-mapping")]
    [ProducesResponseType<ResponseDataDto<PartyMappingResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMyPartyMapping([FromBody] SetPartyMappingRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var setPartyMappingRequestDto = new SetPartyMappingRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await setPartyMappingRequestValidator.ValidateAndThrowAsync(setPartyMappingRequestDto, HttpContext.RequestAborted);

        var partyMapping = await partyMappingService.SetAsync(new SetPartyMappingRequest
        {
            OrganizationId = new OrganizationId(authenticatedUser.OrganizationId),
            Mappings = body.Mappings.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await partyMappingResponseFactory.CreateAsync(
            partyMapping, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Idempotent]
    [HttpDelete("/organizations/my/party-mapping")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyPartyMapping()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await partyMappingRequestValidator.ValidateAndThrowAsync(
            new PartyMappingRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await partyMappingService.DeleteAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.PartyMappingsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/party-mapping/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverMyPartyMapping()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await recoverPartyMappingRequestValidator.ValidateAndThrowAsync(
            new RecoverPartyMappingRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await partyMappingService.RecoverAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("/organizations/my/party-mapping/versions")]
    [ProducesResponseType<PagedResponseDto<PartyMappingVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyPartyMappingVersions([FromQuery] ListMyPartyMappingVersionRequestDto listMyPartyMappingVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyPartyMappingVersionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listMyPartyMappingVersionRequestValidator.ValidateAndThrowAsync(listMyPartyMappingVersionRequestDto, HttpContext.RequestAborted);

        var versions = await partyMappingService.ListAsync(listMyPartyMappingVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await partyMappingResponseFactory.CreateAsync(
            versions.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(versions.ToDto(resources));
    }

    [HttpGet("/organizations/my/party-mapping/versions/{versionNumber:int}")]
    [ProducesResponseType<ResponseDataDto<PartyMappingVersionResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPartyMappingVersion(int versionNumber)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var partyMappingVersionRequestDto = new PartyMappingVersionRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            VersionNumber = versionNumber
        };

        await partyMappingVersionRequestValidator.ValidateAndThrowAsync(partyMappingVersionRequestDto, HttpContext.RequestAborted);

        var version = await partyMappingService.GetVersionAsync(
            new OrganizationId(authenticatedUser.OrganizationId), new VersionNumber(versionNumber), HttpContext.RequestAborted);

        var resource = await partyMappingResponseFactory.CreateAsync(
            version, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [HttpGet("/organizations/my/party-mapping/usages")]
    [ProducesResponseType<ResponseDataDto<PartyMappingUsageResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyPartyMappingUsages()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var usages = await partyMappingService.ListUsagesAsync(
            new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return Ok(usages.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.PartyMappingsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("/organizations/my/party-mapping/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPartyMappingHistory([FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await partyMappingHistoryRequestValidator.ValidateAndThrowAsync(
            new PartyMappingHistoryRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        var partyMapping = await partyMappingService.GetIncludingDeletedAsync(
            new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.PartyMapping, partyMapping.PartyMappingId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    private Task<PartyMapping> ReadAsync(OrganizationId organizationId, bool includeDeleted)
    {
        return includeDeleted
            ? partyMappingService.GetIncludingDeletedAsync(organizationId, HttpContext.RequestAborted)
            : partyMappingService.GetAsync(organizationId, HttpContext.RequestAborted);
    }
}
