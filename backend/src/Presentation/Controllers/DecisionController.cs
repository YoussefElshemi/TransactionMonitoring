using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Decisions;
using Presentation.Models.Decisions;

namespace Presentation.Controllers;

[RequirePermission(Permission.DecisionsResolve)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("decisions")]
public class DecisionController(
    IApplicationUserService applicationUserService,
    IDecisionService decisionService,
    IValidator<ResolveDecisionRequestDto> resolveDecisionRequestValidator) : Controller
{
    [Idempotent]
    [HttpPost("{DecisionId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResolveDecision(ResolveDecisionRequestDto resolveDecisionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        resolveDecisionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        resolveDecisionRequestDto.Body.UserId = authenticatedUser.UserId;
        await resolveDecisionRequestValidator.ValidateAndThrowAsync(resolveDecisionRequestDto, HttpContext.RequestAborted);

        await decisionService.ResolveAsync(resolveDecisionRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
