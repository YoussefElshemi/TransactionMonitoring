using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.VelocityProfiles;
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
using Presentation.Mappers.VelocityProfiles;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.VelocityProfiles;

namespace Presentation.Controllers;

[RequirePermission(Permission.VelocityProfilesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/velocity-profile")]
public class OrganizationVelocityProfileController(
    IApplicationUserService applicationUserService,
    IVelocityProfileService velocityProfileService,
    IAuditService auditService,
    VelocityProfileResponseFactory velocityProfileResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<SetVelocityProfileRequestDto> setVelocityProfileRequestValidator,
    IValidator<VelocityProfileRequestDto> velocityProfileRequestValidator,
    IValidator<GetVelocityProfileRequestDto> getVelocityProfileRequestValidator,
    IValidator<RecoverVelocityProfileRequestDto> recoverVelocityProfileRequestValidator,
    IValidator<VelocityProfileHistoryRequestDto> velocityProfileHistoryRequestValidator,
    IValidator<ListVelocityBackfillRequestDto> listVelocityBackfillRequestValidator,
    IValidator<ListMyVelocityBackfillRequestDto> listMyVelocityBackfillRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<VelocityProfileResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocityProfile([FromQuery] GetVelocityProfileRequestDto getVelocityProfileRequestDto, Guid organizationId)
    {
        getVelocityProfileRequestDto = getVelocityProfileRequestDto with { OrganizationId = organizationId };

        await getVelocityProfileRequestValidator.ValidateAndThrowAsync(getVelocityProfileRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var velocityProfile = await ReadAsync(new OrganizationId(organizationId), getVelocityProfileRequestDto.IncludeDeleted);

        var resource = await velocityProfileResponseFactory.CreateAsync(velocityProfile, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType<ResponseDataDto<VelocityProfileResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetVelocityProfile(SetVelocityProfileRequestDto setVelocityProfileRequestDto)
    {
        await setVelocityProfileRequestValidator.ValidateAndThrowAsync(setVelocityProfileRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var velocityProfile = await velocityProfileService.SetAsync(new SetVelocityProfileRequest
        {
            OrganizationId = new OrganizationId(setVelocityProfileRequestDto.OrganizationId),
            Metrics = setVelocityProfileRequestDto.Body.Metrics.ToModel(),
            Thresholds = setVelocityProfileRequestDto.Body.Thresholds.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await velocityProfileResponseFactory.CreateAsync(
            velocityProfile, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVelocityProfile(VelocityProfileRequestDto velocityProfileRequestDto)
    {
        await velocityProfileRequestValidator.ValidateAndThrowAsync(velocityProfileRequestDto, HttpContext.RequestAborted);

        await velocityProfileService.DeleteAsync(new OrganizationId(velocityProfileRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverVelocityProfile(RecoverVelocityProfileRequestDto recoverVelocityProfileRequestDto)
    {
        await recoverVelocityProfileRequestValidator.ValidateAndThrowAsync(recoverVelocityProfileRequestDto, HttpContext.RequestAborted);

        await velocityProfileService.RecoverAsync(new OrganizationId(recoverVelocityProfileRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("usages")]
    [ProducesResponseType<ResponseDataDto<VelocityProfileUsageResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVelocityProfileUsages(Guid organizationId)
    {
        await velocityProfileHistoryRequestValidator.ValidateAndThrowAsync(
            new VelocityProfileHistoryRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var usages = await velocityProfileService.ListUsagesAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        return Ok(usages.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("backfills")]
    [ProducesResponseType<PagedResponseDto<VelocityBackfillResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVelocityBackfills([FromQuery] ListVelocityBackfillRequestDto listVelocityBackfillRequestDto)
    {
        await listVelocityBackfillRequestValidator.ValidateAndThrowAsync(listVelocityBackfillRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var backfills = await velocityProfileService.ListAsync(listVelocityBackfillRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await velocityProfileResponseFactory.CreateAsync(backfills.Data, context, HttpContext.RequestAborted);

        return Ok(backfills.ToDto(resources));
    }

    [RequirePermission(Permission.VelocityProfilesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocityProfileHistory(Guid organizationId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        await velocityProfileHistoryRequestValidator.ValidateAndThrowAsync(
            new VelocityProfileHistoryRequestDto { OrganizationId = organizationId }, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var velocityProfile = await velocityProfileService.GetIncludingDeletedAsync(new OrganizationId(organizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.VelocityProfile, velocityProfile.VelocityProfileId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [HttpGet("/organizations/my/velocity-profile")]
    [ProducesResponseType<ResponseDataDto<VelocityProfileResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVelocityProfile([FromQuery] bool includeDeleted)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await getVelocityProfileRequestValidator.ValidateAndThrowAsync(new GetVelocityProfileRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            IncludeDeleted = includeDeleted
        }, HttpContext.RequestAborted);

        var velocityProfile = await ReadAsync(new OrganizationId(authenticatedUser.OrganizationId), includeDeleted);

        var resource = await velocityProfileResponseFactory.CreateAsync(
            velocityProfile, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Idempotent]
    [HttpPut("/organizations/my/velocity-profile")]
    [ProducesResponseType<ResponseDataDto<VelocityProfileResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMyVelocityProfile([FromBody] SetVelocityProfileRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var setVelocityProfileRequestDto = new SetVelocityProfileRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await setVelocityProfileRequestValidator.ValidateAndThrowAsync(setVelocityProfileRequestDto, HttpContext.RequestAborted);

        var velocityProfile = await velocityProfileService.SetAsync(new SetVelocityProfileRequest
        {
            OrganizationId = new OrganizationId(authenticatedUser.OrganizationId),
            Metrics = body.Metrics.ToModel(),
            Thresholds = body.Thresholds.ToModel(),
            CreatedByUserId = authenticatedUser.UserId
        }, HttpContext.RequestAborted);

        var resource = await velocityProfileResponseFactory.CreateAsync(
            velocityProfile, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Idempotent]
    [HttpDelete("/organizations/my/velocity-profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyVelocityProfile()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await velocityProfileRequestValidator.ValidateAndThrowAsync(
            new VelocityProfileRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await velocityProfileService.DeleteAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.VelocityProfilesWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/velocity-profile/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverMyVelocityProfile()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await recoverVelocityProfileRequestValidator.ValidateAndThrowAsync(
            new RecoverVelocityProfileRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        await velocityProfileService.RecoverAsync(new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("/organizations/my/velocity-profile/usages")]
    [ProducesResponseType<ResponseDataDto<VelocityProfileUsageResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyVelocityProfileUsages()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var usages = await velocityProfileService.ListUsagesAsync(
            new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        return Ok(usages.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [HttpGet("/organizations/my/velocity-profile/backfills")]
    [ProducesResponseType<PagedResponseDto<VelocityBackfillResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyVelocityBackfills([FromQuery] ListMyVelocityBackfillRequestDto listMyVelocityBackfillRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyVelocityBackfillRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listMyVelocityBackfillRequestValidator.ValidateAndThrowAsync(listMyVelocityBackfillRequestDto, HttpContext.RequestAborted);

        var backfills = await velocityProfileService.ListAsync(listMyVelocityBackfillRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await velocityProfileResponseFactory.CreateAsync(
            backfills.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(backfills.ToDto(resources));
    }

    [RequirePermission(Permission.VelocityProfilesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("/organizations/my/velocity-profile/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVelocityProfileHistory([FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        await velocityProfileHistoryRequestValidator.ValidateAndThrowAsync(
            new VelocityProfileHistoryRequestDto { OrganizationId = authenticatedUser.OrganizationId }, HttpContext.RequestAborted);

        var velocityProfile = await velocityProfileService.GetIncludingDeletedAsync(
            new OrganizationId(authenticatedUser.OrganizationId), HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.VelocityProfile, velocityProfile.VelocityProfileId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    private Task<VelocityProfile> ReadAsync(OrganizationId organizationId, bool includeDeleted)
    {
        return includeDeleted
            ? velocityProfileService.GetIncludingDeletedAsync(organizationId, HttpContext.RequestAborted)
            : velocityProfileService.GetAsync(organizationId, HttpContext.RequestAborted);
    }
}
