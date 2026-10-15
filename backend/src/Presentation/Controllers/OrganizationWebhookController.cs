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
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Controllers;

[RequirePermission(Permission.WebhookConfigurationsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/webhook-configurations")]
public class OrganizationWebhookController(
    IApplicationUserService applicationUserService,
    IWebhookConfigurationService webhookConfigurationService,
    IWebhookHealthService webhookHealthService,
    IAuditService auditService,
    WebhookConfigurationResponseFactory webhookConfigurationResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<ListOrganizationWebhookConfigurationRequestDto> listOrganizationWebhookConfigurationRequestValidator,
    IValidator<ListMyOrganizationWebhookConfigurationRequestDto> listMyOrganizationWebhookConfigurationRequestValidator,
    IValidator<CreateWebhookConfigurationRequestDto> createWebhookConfigurationRequestValidator,
    IValidator<OrganizationWebhookConfigurationRequestDto> organizationWebhookConfigurationRequestValidator,
    IValidator<RecoverOrganizationWebhookConfigurationRequestDto> recoverOrganizationWebhookConfigurationRequestValidator,
    IValidator<UpdateOrganizationWebhookConfigurationRequestDto> updateOrganizationWebhookConfigurationRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<WebhookConfigurationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWebhookConfigurations([FromQuery] ListOrganizationWebhookConfigurationRequestDto listOrganizationWebhookConfigurationRequestDto)
    {
        await listOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(listOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var webhookConfigurations = await webhookConfigurationService.ListAsync(listOrganizationWebhookConfigurationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookConfigurationResponseFactory.CreateAsync(
            webhookConfigurations.Data, context, HttpContext.RequestAborted);

        return Ok(webhookConfigurations.ToDto(resources));
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationSecretResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWebhookConfigurations(CreateWebhookConfigurationRequestDto createWebhookConfigurationRequestDto)
    {
        await createWebhookConfigurationRequestValidator.ValidateAndThrowAsync(createWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var createWebhookConfigurationRequest = createWebhookConfigurationRequestDto.ToModel();

        var webhookConfiguration = await webhookConfigurationService.CreateAsync(createWebhookConfigurationRequest, HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, webhookConfiguration.ToSecretDto().ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{WebhookConfigurationId:guid}/rotate-secret")]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationSecretResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateWebhookConfigurationSecret(OrganizationWebhookConfigurationRequestDto organizationWebhookConfigurationRequestDto)
    {
        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var webhookConfiguration = await webhookConfigurationService.RotateSecretAsync(
            new WebhookConfigurationId(organizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        return Ok(webhookConfiguration.ToSecretDto().ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{WebhookConfigurationId:guid}")]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookConfiguration(OrganizationWebhookConfigurationRequestDto organizationWebhookConfigurationRequestDto)
    {
        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var webhookConfiguration = await webhookConfigurationService.GetAsync(new WebhookConfigurationId(organizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        var resource = await webhookConfigurationResponseFactory.CreateAsync(
            webhookConfiguration, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("{WebhookConfigurationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateWebhookConfiguration(UpdateOrganizationWebhookConfigurationRequestDto updateOrganizationWebhookConfigurationRequestDto)
    {
        await updateOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(updateOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.UpdateAsync(updateOrganizationWebhookConfigurationRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{WebhookConfigurationId:guid}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestWebhookConfiguration(OrganizationWebhookConfigurationRequestDto organizationWebhookConfigurationRequestDto)
    {
        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var result = await webhookConfigurationService.TestAsync(new WebhookConfigurationId(organizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        if (result)
        {
            return Ok();
        }

        return BadRequest();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{WebhookConfigurationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWebhookConfiguration(OrganizationWebhookConfigurationRequestDto organizationWebhookConfigurationRequestDto)
    {
        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.DeleteAsync(new WebhookConfigurationId(organizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{WebhookConfigurationId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverWebhookConfiguration(RecoverOrganizationWebhookConfigurationRequestDto recoverOrganizationWebhookConfigurationRequestDto)
    {
        await recoverOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(recoverOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.RecoverAsync(new WebhookConfigurationId(recoverOrganizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{webhookConfigurationId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookConfigurationHistory(
        Guid organizationId,
        Guid webhookConfigurationId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.WebhookConfiguration, webhookConfigurationId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }

    [HttpGet("/organizations/my/webhook-configurations")]
    [ProducesResponseType<PagedResponseDto<WebhookConfigurationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyWebhookConfigurations([FromQuery] ListMyOrganizationWebhookConfigurationRequestDto listMyOrganizationWebhookConfigurationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyOrganizationWebhookConfigurationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listMyOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(listMyOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var webhookConfigurations = await webhookConfigurationService.ListAsync(listMyOrganizationWebhookConfigurationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await webhookConfigurationResponseFactory.CreateAsync(
            webhookConfigurations.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(webhookConfigurations.ToDto(resources));
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-configurations")]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationSecretResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMyWebhookConfiguration([FromBody] CreateWebhookConfigurationRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var createWebhookConfigurationRequestDto = new CreateWebhookConfigurationRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await createWebhookConfigurationRequestValidator.ValidateAndThrowAsync(createWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var createWebhookConfigurationRequest = createWebhookConfigurationRequestDto.ToModel();

        var webhookConfiguration = await webhookConfigurationService.CreateAsync(createWebhookConfigurationRequest, HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, webhookConfiguration.ToSecretDto().ToResponseData());
    }

    [HttpGet("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}")]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWebhookConfiguration(Guid webhookConfigurationId)
    {
        var organizationWebhookConfigurationRequestDto = await BuildMyWebhookConfigurationRequestAsync(webhookConfigurationId);

        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var webhookConfiguration = await webhookConfigurationService.GetAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        var resource = await webhookConfigurationResponseFactory.CreateAsync(
            webhookConfiguration, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPatch("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMyWebhookConfiguration(Guid webhookConfigurationId, [FromBody] UpdateWebhookConfigurationRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var updateOrganizationWebhookConfigurationRequestDto = new UpdateOrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            WebhookConfigurationId = webhookConfigurationId,
            Body = body
        };

        await updateOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(updateOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.UpdateAsync(updateOrganizationWebhookConfigurationRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpDelete("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyWebhookConfiguration(Guid webhookConfigurationId)
    {
        var organizationWebhookConfigurationRequestDto = await BuildMyWebhookConfigurationRequestAsync(webhookConfigurationId);

        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.DeleteAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverMyWebhookConfiguration(Guid webhookConfigurationId)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var recoverOrganizationWebhookConfigurationRequestDto = new RecoverOrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            WebhookConfigurationId = webhookConfigurationId
        };

        await recoverOrganizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(recoverOrganizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        await webhookConfigurationService.RecoverAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestMyWebhookConfiguration(Guid webhookConfigurationId)
    {
        var organizationWebhookConfigurationRequestDto = await BuildMyWebhookConfigurationRequestAsync(webhookConfigurationId);

        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var result = await webhookConfigurationService.TestAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        if (result)
        {
            return Ok();
        }

        return BadRequest();
    }

    [RequirePermission(Permission.WebhookConfigurationsWrite)]
    [Idempotent]
    [HttpPost("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}/rotate-secret")]
    [ProducesResponseType<ResponseDataDto<WebhookConfigurationSecretResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateMyWebhookConfigurationSecret(Guid webhookConfigurationId)
    {
        var organizationWebhookConfigurationRequestDto = await BuildMyWebhookConfigurationRequestAsync(webhookConfigurationId);

        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var webhookConfiguration = await webhookConfigurationService.RotateSecretAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        return Ok(webhookConfiguration.ToSecretDto().ToResponseData());
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{WebhookConfigurationId:guid}/health")]
    [ProducesResponseType<ResponseDataDto<WebhookEndpointHealthResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookConfigurationHealth(OrganizationWebhookConfigurationRequestDto organizationWebhookConfigurationRequestDto)
    {
        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var health = await webhookHealthService.GetAsync(
            new WebhookConfigurationId(organizationWebhookConfigurationRequestDto.WebhookConfigurationId), HttpContext.RequestAborted);

        return Ok(health.ToDto().ToResponseData());
    }

    [HttpGet("/organizations/my/webhook-configurations/{webhookConfigurationId:guid}/health")]
    [ProducesResponseType<ResponseDataDto<WebhookEndpointHealthResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWebhookConfigurationHealth(Guid webhookConfigurationId)
    {
        var organizationWebhookConfigurationRequestDto = await BuildMyWebhookConfigurationRequestAsync(webhookConfigurationId);

        await organizationWebhookConfigurationRequestValidator.ValidateAndThrowAsync(organizationWebhookConfigurationRequestDto, HttpContext.RequestAborted);

        var health = await webhookHealthService.GetAsync(new WebhookConfigurationId(webhookConfigurationId), HttpContext.RequestAborted);

        return Ok(health.ToDto().ToResponseData());
    }

    private async Task<OrganizationWebhookConfigurationRequestDto> BuildMyWebhookConfigurationRequestAsync(Guid webhookConfigurationId)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        return new OrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            WebhookConfigurationId = webhookConfigurationId
        };
    }
}