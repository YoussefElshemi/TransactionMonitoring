using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Organizations;
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
using Presentation.Mappers.Organizations;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
// Controller and action attributes are ANDed, so roles live on each action.
[RequirePermission(Permission.OrganizationsRead)]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations")]
public class OrganizationController(
    IApplicationUserService applicationUserService,
    IOrganizationService organizationService,
    IAuditService auditService,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CreateOrganizationRequestDto> createOrganizationRequestValidator,
    IValidator<OrganizationRequestDto> organizationRequestValidator,
    IValidator<RecoverOrganizationRequestDto> recoverOrganizationRequestValidator,
    IValidator<UpdateOrganizationRequestDto> updateOrganizationRequestValidator,
    IValidator<UpdateOrganizationHoldPolicyRequestDto> updateOrganizationHoldPolicyRequestValidator,
    IValidator<UpdateOrganizationAlertPolicyRequestDto> updateOrganizationAlertPolicyRequestValidator,
    IValidator<ListOrganizationRequestDto> listOrganizationRequestValidator) : Controller
{
    [RequirePermission(Permission.OrganizationsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<OrganizationResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequestDto createOrganizationRequestDto)
    {
        await createOrganizationRequestValidator.ValidateAndThrowAsync(createOrganizationRequestDto, HttpContext.RequestAborted);

        var createOrganizationRequest = new CreateOrganizationRequest
        {
            FriendlyName = new FriendlyName(createOrganizationRequestDto.FriendlyName),
            Status = OrganizationStatus.Enabled,
            HoldWindow = null,
            HoldExpiryVerdict = null,
            DeduplicationWindow = null,
            CaseGroupingPeriod = null
        };

        var organization = await organizationService.CreateAsync(createOrganizationRequest, HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, organization.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.OrganizationsRead)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<OrganizationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrganizations([FromQuery] ListOrganizationRequestDto listOrganizationRequestDto)
    {
        await listOrganizationRequestValidator.ValidateAndThrowAsync(listOrganizationRequestDto, HttpContext.RequestAborted);

        var organizations = await organizationService.ListAsync(listOrganizationRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(organizations.ToDto(OrganizationDtoMapper.ToDto));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{OrganizationId:guid}")]
    [ProducesResponseType<ResponseDataDto<OrganizationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganization(OrganizationRequestDto organizationRequestDto)
    {
        await organizationRequestValidator.ValidateAndThrowAsync(organizationRequestDto, HttpContext.RequestAborted);

        var organization = await organizationService.GetAsync(new OrganizationId(organizationRequestDto.OrganizationId), HttpContext.RequestAborted);

        return Ok(organization.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("{OrganizationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrganization(UpdateOrganizationRequestDto updateOrganizationRequestDto)
    {
        await updateOrganizationRequestValidator.ValidateAndThrowAsync(updateOrganizationRequestDto, HttpContext.RequestAborted);

        await organizationService.UpdateAsync(updateOrganizationRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("my")]
    [ProducesResponseType<ResponseDataDto<OrganizationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrganization()
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var organization = await organizationService.GetAsync(authenticatedUser.OrganizationId, HttpContext.RequestAborted);

        return Ok(organization.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("my/alert-policy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMyOrganizationAlertPolicy([FromBody] UpdateOrganizationAlertPolicyRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var updateOrganizationAlertPolicyRequestDto = new UpdateOrganizationAlertPolicyRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await updateOrganizationAlertPolicyRequestValidator.ValidateAndThrowAsync(updateOrganizationAlertPolicyRequestDto, HttpContext.RequestAborted);

        await organizationService.UpdateAsync(updateOrganizationAlertPolicyRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("{OrganizationId:guid}/alert-policy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrganizationAlertPolicy(UpdateOrganizationAlertPolicyRequestDto updateOrganizationAlertPolicyRequestDto)
    {
        await updateOrganizationAlertPolicyRequestValidator.ValidateAndThrowAsync(updateOrganizationAlertPolicyRequestDto, HttpContext.RequestAborted);

        await organizationService.UpdateAsync(updateOrganizationAlertPolicyRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("my/hold-policy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMyOrganizationHoldPolicy([FromBody] UpdateOrganizationHoldPolicyRequestBodyDto body)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var updateOrganizationHoldPolicyRequestDto = new UpdateOrganizationHoldPolicyRequestDto
        {
            OrganizationId = authenticatedUser.OrganizationId,
            Body = body
        };

        await updateOrganizationHoldPolicyRequestValidator.ValidateAndThrowAsync(updateOrganizationHoldPolicyRequestDto, HttpContext.RequestAborted);

        await organizationService.UpdateAsync(updateOrganizationHoldPolicyRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{OrganizationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOrganization(OrganizationRequestDto organizationRequestDto)
    {
        await organizationRequestValidator.ValidateAndThrowAsync(organizationRequestDto, HttpContext.RequestAborted);

        await organizationService.DeleteAsync(new OrganizationId(organizationRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.OrganizationsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{OrganizationId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverOrganization(RecoverOrganizationRequestDto recoverOrganizationRequestDto)
    {
        await recoverOrganizationRequestValidator.ValidateAndThrowAsync(recoverOrganizationRequestDto, HttpContext.RequestAborted);

        await organizationService.RecoverAsync(new OrganizationId(recoverOrganizationRequestDto.OrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.OrganizationsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{organizationId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganizationHistory(Guid organizationId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Organization, organizationId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}