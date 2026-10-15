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
using Presentation.Mappers.ApiClients;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Models.ApiClients;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.ApiClientsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/api-clients")]
public class OrganizationApiClientController(
    IApplicationUserService applicationUserService,
    IApiClientService apiClientService,
    IAuditService auditService,
    ApiClientResponseFactory apiClientResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationListApiClientRequestDto> organizationListApiClientRequestValidator,
    IValidator<OrganizationApiClientRequestDto> organizationApiClientRequestValidator,
    IValidator<OrganizationUpdateApiClientRequestDto> organizationUpdateApiClientRequestValidator,
    IValidator<OrganizationRecoverApiClientRequestDto> organizationRecoverApiClientRequestValidator,
    IValidator<OrganizationApiClientSecretRequestDto> organizationApiClientSecretRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ApiClientResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiClients([FromQuery] OrganizationListApiClientRequestDto organizationListApiClientRequestDto)
    {
        await organizationListApiClientRequestValidator.ValidateAndThrowAsync(
            organizationListApiClientRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var apiClients = await apiClientService.ListAsync(organizationListApiClientRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await apiClientResponseFactory.CreateAsync(apiClients.Data, context, HttpContext.RequestAborted);

        return Ok(apiClients.ToDto(resources));
    }

    [HttpGet("{ApiClientId:guid}")]
    [ProducesResponseType<ResponseDataDto<ApiClientResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClient(OrganizationApiClientRequestDto organizationApiClientRequestDto)
    {
        await organizationApiClientRequestValidator.ValidateAndThrowAsync(
            organizationApiClientRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var apiClient = await apiClientService.GetAsync(
            new ApiClientId(organizationApiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        var resource = await apiClientResponseFactory.CreateAsync(apiClient, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPatch("{ApiClientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateApiClient(OrganizationUpdateApiClientRequestDto organizationUpdateApiClientRequestDto)
    {
        await organizationUpdateApiClientRequestValidator.ValidateAndThrowAsync(
            organizationUpdateApiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.UpdateAsync(organizationUpdateApiClientRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpDelete("{ApiClientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteApiClient(OrganizationApiClientRequestDto organizationApiClientRequestDto)
    {
        await organizationApiClientRequestValidator.ValidateAndThrowAsync(
            organizationApiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.DeleteAsync(
            new ApiClientId(organizationApiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPost("{ApiClientId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverApiClient(OrganizationRecoverApiClientRequestDto organizationRecoverApiClientRequestDto)
    {
        await organizationRecoverApiClientRequestValidator.ValidateAndThrowAsync(
            organizationRecoverApiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.RecoverAsync(
            new ApiClientId(organizationRecoverApiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{ApiClientId:guid}/secrets")]
    [ProducesResponseType<ResponseDataDto<List<ApiClientSecretResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiClientSecrets(OrganizationApiClientRequestDto organizationApiClientRequestDto)
    {
        await organizationApiClientRequestValidator.ValidateAndThrowAsync(
            organizationApiClientRequestDto, HttpContext.RequestAborted);

        var apiClientSecrets = await apiClientService.ListSecretsAsync(
            new ApiClientId(organizationApiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return Ok(apiClientSecrets.ConvertAll(x => x.ToDto()).ToResponseData());
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpDelete("{ApiClientId:guid}/secrets/{ApiClientSecretId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeApiClientSecret(OrganizationApiClientSecretRequestDto organizationApiClientSecretRequestDto)
    {
        await organizationApiClientSecretRequestValidator.ValidateAndThrowAsync(
            organizationApiClientSecretRequestDto, HttpContext.RequestAborted);

        await apiClientService.RevokeSecretAsync(
            new ApiClientSecretId(organizationApiClientSecretRequestDto.ApiClientSecretId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{apiClientId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClientHistory(
        Guid organizationId,
        Guid apiClientId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.ApiClient, apiClientId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [RequirePermission(Permission.ApiClientsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{apiClientId:guid}/secrets/{apiClientSecretId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClientSecretHistory(
        Guid organizationId,
        Guid apiClientSecretId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.ApiClientSecret, apiClientSecretId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
