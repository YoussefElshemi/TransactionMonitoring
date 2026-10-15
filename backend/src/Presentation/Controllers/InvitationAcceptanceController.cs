using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Mappers.Common;
using Presentation.Mappers.Invitations;
using Presentation.Models.Common;
using Presentation.Models.Invitations;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[AllowAnonymous]
[Route("invitations")]
public class InvitationAcceptanceController(
    IInvitationService invitationService,
    IOrganizationService organizationService,
    IValidator<PreviewInvitationRequestDto> previewInvitationRequestValidator,
    IValidator<AcceptInvitationRequestDto> acceptInvitationRequestValidator) : Controller
{
    [HttpGet("preview")]
    [ProducesResponseType<ResponseDataDto<InvitationPreviewResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewInvitation([FromQuery] PreviewInvitationRequestDto previewInvitationRequestDto)
    {
        await previewInvitationRequestValidator.ValidateAndThrowAsync(previewInvitationRequestDto, HttpContext.RequestAborted);

        var invitation = await invitationService.PreviewAsync(
            new InvitationToken(previewInvitationRequestDto.Token), HttpContext.RequestAborted);

        var organization = await organizationService.GetAsync(invitation.OrganizationId, HttpContext.RequestAborted);

        return Ok(invitation.ToPreviewDto(organization).ToResponseData());
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the organisation this would be keyed against by calling it.")]
    [HttpPost("accept")]
    [ProducesResponseType<ResponseDataDto<AcceptedInvitationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequestDto acceptInvitationRequestDto)
    {
        await acceptInvitationRequestValidator.ValidateAndThrowAsync(acceptInvitationRequestDto, HttpContext.RequestAborted);

        var acceptedInvitation = await invitationService.AcceptAsync(
            acceptInvitationRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(acceptedInvitation.ToDto().ToResponseData());
    }
}
