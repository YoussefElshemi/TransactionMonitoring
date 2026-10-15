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
[ValidateAuthenticatedUser]
[Route("invitations")]
public class InvitationController(
    IApplicationUserService applicationUserService,
    IInvitationService invitationService,
    IInvitationNotifier invitationNotifier,
    InvitationResponseFactory invitationResponseFactory,
    IValidator<CreateInvitationRequestDto> createInvitationRequestValidator,
    IValidator<ListInvitationRequestDto> listInvitationRequestValidator,
    IValidator<InvitationRequestDto> invitationRequestValidator,
    IValidator<ResendInvitationRequestDto> resendInvitationRequestValidator,
    IValidator<RevokeInvitationRequestDto> revokeInvitationRequestValidator) : Controller
{
    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<IssuedInvitationResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequestDto createInvitationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createInvitationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createInvitationRequestValidator.ValidateAndThrowAsync(createInvitationRequestDto, HttpContext.RequestAborted);

        var issuedInvitation = await invitationService.InviteAsync(
            createInvitationRequestDto.ToModel(authenticatedUser.OrganizationId, authenticatedUser.UserId),
            HttpContext.RequestAborted);

        await invitationNotifier.NotifyAsync(issuedInvitation, HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(
            issuedInvitation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<InvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvitations([FromQuery] ListInvitationRequestDto listInvitationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listInvitationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listInvitationRequestValidator.ValidateAndThrowAsync(listInvitationRequestDto, HttpContext.RequestAborted);

        var invitations = await invitationService.ListAsync(listInvitationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await invitationResponseFactory.CreateAsync(
            invitations.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(invitations.ToDto(resources));
    }

    [HttpGet("{InvitationId:guid}")]
    [ProducesResponseType<ResponseDataDto<InvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitation(InvitationRequestDto invitationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        invitationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await invitationRequestValidator.ValidateAndThrowAsync(invitationRequestDto, HttpContext.RequestAborted);

        var invitation = await invitationService.GetAsync(new InvitationId(invitationRequestDto.InvitationId), HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(
            invitation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost("{InvitationId:guid}/resend")]
    [ProducesResponseType<ResponseDataDto<IssuedInvitationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendInvitation(ResendInvitationRequestDto resendInvitationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        resendInvitationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await resendInvitationRequestValidator.ValidateAndThrowAsync(resendInvitationRequestDto, HttpContext.RequestAborted);

        var issuedInvitation = await invitationService.ResendAsync(
            new InvitationId(resendInvitationRequestDto.InvitationId), HttpContext.RequestAborted);

        await invitationNotifier.NotifyAsync(issuedInvitation, HttpContext.RequestAborted);

        var resource = await invitationResponseFactory.CreateAsync(
            issuedInvitation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.InvitationsWrite)]
    [Idempotent]
    [HttpPost("{InvitationId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeInvitation(RevokeInvitationRequestDto revokeInvitationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        revokeInvitationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await revokeInvitationRequestValidator.ValidateAndThrowAsync(revokeInvitationRequestDto, HttpContext.RequestAborted);

        await invitationService.RevokeAsync(
            new InvitationId(revokeInvitationRequestDto.InvitationId), authenticatedUser.UserId, HttpContext.RequestAborted);

        return NoContent();
    }
}
