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
using Presentation.Mappers.Common;
using Presentation.Mappers.Invitations;
using Presentation.Models.Common;
using Presentation.Models.Invitations;

namespace Presentation.Controllers;

[RequirePermission(Permission.InvitationsRead)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/invitations")]
public class OrganizationInvitationController(
    IApplicationUserService applicationUserService,
    IInvitationService invitationService,
    IInvitationNotifier invitationNotifier,
    InvitationResponseFactory invitationResponseFactory,
    IValidator<OrganizationCreateInvitationRequestDto> organizationCreateInvitationRequestValidator,
    IValidator<OrganizationListInvitationRequestDto> organizationListInvitationRequestValidator,
    IValidator<OrganizationInvitationRequestDto> organizationInvitationRequestValidator) : Controller
{
    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<IssuedInvitationResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrganizationInvitation(
        OrganizationCreateInvitationRequestDto organizationCreateInvitationRequestDto)
    {
        await organizationCreateInvitationRequestValidator.ValidateAndThrowAsync(
            organizationCreateInvitationRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var issuedInvitation = await invitationService.InviteAsync(
            organizationCreateInvitationRequestDto.Body.ToModel(
                new OrganizationId(organizationCreateInvitationRequestDto.OrganizationId), authenticatedUser.UserId),
            HttpContext.RequestAborted);

        await invitationNotifier.NotifyAsync(issuedInvitation, HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(
            issuedInvitation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<InvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrganizationInvitations(
        [FromQuery] OrganizationListInvitationRequestDto organizationListInvitationRequestDto)
    {
        await organizationListInvitationRequestValidator.ValidateAndThrowAsync(
            organizationListInvitationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var invitations = await invitationService.ListAsync(organizationListInvitationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await invitationResponseFactory.CreateAsync(invitations.Data, context, HttpContext.RequestAborted);

        return Ok(invitations.ToDto(resources));
    }

    [HttpGet("{InvitationId:guid}")]
    [ProducesResponseType<ResponseDataDto<InvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganizationInvitation(OrganizationInvitationRequestDto organizationInvitationRequestDto)
    {
        await organizationInvitationRequestValidator.ValidateAndThrowAsync(
            organizationInvitationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var invitation = await invitationService.GetAsync(
            new InvitationId(organizationInvitationRequestDto.InvitationId), HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(invitation, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost("{InvitationId:guid}/resend")]
    [ProducesResponseType<ResponseDataDto<IssuedInvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendOrganizationInvitation(OrganizationInvitationRequestDto organizationInvitationRequestDto)
    {
        await organizationInvitationRequestValidator.ValidateAndThrowAsync(
            organizationInvitationRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var issuedInvitation = await invitationService.ResendAsync(
            new InvitationId(organizationInvitationRequestDto.InvitationId), HttpContext.RequestAborted);

        await invitationNotifier.NotifyAsync(issuedInvitation, HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(issuedInvitation, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost("{InvitationId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeOrganizationInvitation(OrganizationInvitationRequestDto organizationInvitationRequestDto)
    {
        await organizationInvitationRequestValidator.ValidateAndThrowAsync(
            organizationInvitationRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await invitationService.RevokeAsync(
            new InvitationId(organizationInvitationRequestDto.InvitationId), authenticatedUser.UserId, HttpContext.RequestAborted);

        return NoContent();
    }
}
