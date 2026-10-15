using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.ApiClients;
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
[ValidateAuthenticatedUser]
[Route("api-clients")]
public class ApiClientController(
    IApplicationUserService applicationUserService,
    IApiClientService apiClientService,
    ApiClientResponseFactory apiClientResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IAuditService auditService,
    IValidator<CreateApiClientRequestDto> createApiClientRequestValidator,
    IValidator<ApiClientRequestDto> apiClientRequestValidator,
    IValidator<RecoverApiClientRequestDto> recoverApiClientRequestValidator,
    IValidator<UpdateApiClientRequestDto> updateApiClientRequestValidator,
    IValidator<ListApiClientRequestDto> listApiClientRequestValidator,
    IValidator<CreateApiClientSecretRequestDto> createApiClientSecretRequestValidator,
    IValidator<ApiClientSecretRequestDto> apiClientSecretRequestValidator) : Controller
{
    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<CreatedApiClientResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateApiClient([FromBody] CreateApiClientRequestDto createApiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await createApiClientRequestValidator.ValidateAndThrowAsync(createApiClientRequestDto, HttpContext.RequestAborted);

        var createApiClientRequest = new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName(createApiClientRequestDto.FriendlyName),
            UserId = authenticatedUser.UserId,
            OrganizationId = authenticatedUser.OrganizationId,
            Scopes = createApiClientRequestDto.Scopes.Select(ApiScopeHelper.Parse).ToList()
        };

        var createdApiClient = await apiClientService.CreateAsync(createApiClientRequest, HttpContext.RequestAborted);

        var resource = await apiClientResponseFactory.CreateAsync(
            createdApiClient, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<ApiClientResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiClients([FromQuery] ListApiClientRequestDto listApiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listApiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listApiClientRequestValidator.ValidateAndThrowAsync(listApiClientRequestDto, HttpContext.RequestAborted);

        var apiClients = await apiClientService.ListAsync(listApiClientRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await apiClientResponseFactory.CreateAsync(
            apiClients.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(apiClients.ToDto(resources));
    }

    [HttpGet("{ApiClientId:guid}")]
    [ProducesResponseType<ResponseDataDto<ApiClientResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClient(ApiClientRequestDto apiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        apiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await apiClientRequestValidator.ValidateAndThrowAsync(apiClientRequestDto, HttpContext.RequestAborted);

        var apiClient = await apiClientService.GetAsync(new ApiClientId(apiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        var resource = await apiClientResponseFactory.CreateAsync(
            apiClient, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPatch("{ApiClientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateApiClient(UpdateApiClientRequestDto updateApiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateApiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateApiClientRequestValidator.ValidateAndThrowAsync(updateApiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.UpdateAsync(updateApiClientRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpDelete("{ApiClientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteApiClient(ApiClientRequestDto apiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        apiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await apiClientRequestValidator.ValidateAndThrowAsync(apiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.DeleteAsync(new ApiClientId(apiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPost("{ApiClientId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverApiClient(RecoverApiClientRequestDto recoverApiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverApiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverApiClientRequestValidator.ValidateAndThrowAsync(recoverApiClientRequestDto, HttpContext.RequestAborted);

        await apiClientService.RecoverAsync(new ApiClientId(recoverApiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpPost("{ApiClientId:guid}/secrets")]
    [ProducesResponseType<ResponseDataDto<CreatedApiClientSecretResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateApiClientSecret(CreateApiClientSecretRequestDto createApiClientSecretRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createApiClientSecretRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createApiClientSecretRequestValidator.ValidateAndThrowAsync(createApiClientSecretRequestDto, HttpContext.RequestAborted);

        var createApiClientSecretRequest = new CreateApiClientSecretRequest
        {
            ApiClientId = new ApiClientId(createApiClientSecretRequestDto.ApiClientId),
            OrganizationId = authenticatedUser.OrganizationId,
            UserId = authenticatedUser.UserId,
            ExpiresAt = createApiClientSecretRequestDto.Body.ExpiresAt.HasValue
                ? new ExpiresAt(createApiClientSecretRequestDto.Body.ExpiresAt.Value)
                : null
        };

        var createdApiClientSecret = await apiClientService.CreateSecretAsync(createApiClientSecretRequest, HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, createdApiClientSecret.ToSecretDto().ToResponseData());
    }

    [HttpGet("{ApiClientId:guid}/secrets")]
    [ProducesResponseType<ResponseDataDto<List<ApiClientSecretResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiClientSecrets(ApiClientRequestDto apiClientRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        apiClientRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await apiClientRequestValidator.ValidateAndThrowAsync(apiClientRequestDto, HttpContext.RequestAborted);

        var apiClientSecrets = await apiClientService.ListSecretsAsync(new ApiClientId(apiClientRequestDto.ApiClientId), HttpContext.RequestAborted);

        return Ok(apiClientSecrets.ConvertAll(x => x.ToDto()).ToResponseData());
    }

    [RequirePermission(Permission.ApiClientsWrite)]
    [Idempotent]
    [HttpDelete("{ApiClientId:guid}/secrets/{ApiClientSecretId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeApiClientSecret(ApiClientSecretRequestDto apiClientSecretRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        apiClientSecretRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await apiClientSecretRequestValidator.ValidateAndThrowAsync(apiClientSecretRequestDto, HttpContext.RequestAborted);

        await apiClientService.RevokeSecretAsync(new ApiClientSecretId(apiClientSecretRequestDto.ApiClientSecretId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.ApiClientsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{apiClientId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClientHistory(Guid apiClientId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.ApiClient, apiClientId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [RequirePermission(Permission.ApiClientsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{apiClientId:guid}/secrets/{apiClientSecretId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiClientSecretHistory(Guid apiClientSecretId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.ApiClientSecret, apiClientSecretId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
