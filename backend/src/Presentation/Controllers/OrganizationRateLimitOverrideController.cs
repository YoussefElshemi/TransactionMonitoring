using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.RateLimitOverrides;
using Presentation.Models.Common;
using Presentation.Models.RateLimitOverrides;

namespace Presentation.Controllers;

[RequirePermission(Permission.PlatformAdminister)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/rate-limit-overrides")]
public class OrganizationRateLimitOverrideController(
    IApplicationUserService applicationUserService,
    IRateLimitOverrideService rateLimitOverrideService,
    IValidator<ListRateLimitOverrideRequestDto> listRateLimitOverrideRequestValidator,
    IValidator<CreateRateLimitOverrideRequestDto> createRateLimitOverrideRequestValidator,
    IValidator<DeleteRateLimitOverrideRequestDto> deleteRateLimitOverrideRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<RateLimitOverrideResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRateLimitOverrides([FromQuery] ListRateLimitOverrideRequestDto listRateLimitOverrideRequestDto)
    {
        await listRateLimitOverrideRequestValidator.ValidateAndThrowAsync(listRateLimitOverrideRequestDto, HttpContext.RequestAborted);

        var rateLimitOverrides = await rateLimitOverrideService.ListAsync(
            listRateLimitOverrideRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(rateLimitOverrides.ToDto(x => x.ToDto()));
    }

    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<RateLimitOverrideResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRateLimitOverride(CreateRateLimitOverrideRequestDto createRateLimitOverrideRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createRateLimitOverrideRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createRateLimitOverrideRequestValidator.ValidateAndThrowAsync(createRateLimitOverrideRequestDto, HttpContext.RequestAborted);

        var rateLimitOverride = await rateLimitOverrideService.CreateAsync(
            createRateLimitOverrideRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, rateLimitOverride.ToDto().ToResponseData());
    }

    [Idempotent]
    [HttpDelete("{RateLimitOverrideId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRateLimitOverride(DeleteRateLimitOverrideRequestDto deleteRateLimitOverrideRequestDto)
    {
        await deleteRateLimitOverrideRequestValidator.ValidateAndThrowAsync(deleteRateLimitOverrideRequestDto, HttpContext.RequestAborted);

        await rateLimitOverrideService.DeleteAsync(
            new RateLimitOverrideId(deleteRateLimitOverrideRequestDto.RateLimitOverrideId), HttpContext.RequestAborted);

        return NoContent();
    }
}
