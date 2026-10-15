using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Plans;
using Presentation.Models.Common;
using Presentation.Models.Plans;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("plans")]
public class PlanController(
    IApplicationUserService applicationUserService,
    IPlanService planService,
    IValidator<CreatePlanRequestDto> createPlanRequestValidator,
    IValidator<UpdatePlanRequestDto> updatePlanRequestValidator,
    IValidator<PublishPlanVersionRequestDto> publishPlanVersionRequestValidator,
    IValidator<PlanRequestDto> planRequestValidator,
    IValidator<DeletePlanRequestDto> deletePlanRequestValidator,
    IValidator<ListPlanRequestDto> listPlanRequestValidator,
    IValidator<ListPlanVersionRequestDto> listPlanVersionRequestValidator) : Controller
{
    [RequirePermission(Permission.PlansWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<PlanResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlan(CreatePlanRequestDto createPlanRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createPlanRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createPlanRequestValidator.ValidateAndThrowAsync(createPlanRequestDto, HttpContext.RequestAborted);

        var plan = await planService.CreateAsync(createPlanRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, plan.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PlansRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<PlanResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPlans([FromQuery] ListPlanRequestDto listPlanRequestDto)
    {
        await listPlanRequestValidator.ValidateAndThrowAsync(listPlanRequestDto, HttpContext.RequestAborted);

        var plans = await planService.ListAsync(listPlanRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(plans.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.PlansRead)]
    [HttpGet("{PlanId:guid}")]
    [ProducesResponseType<ResponseDataDto<PlanResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlan(PlanRequestDto planRequestDto)
    {
        await planRequestValidator.ValidateAndThrowAsync(planRequestDto, HttpContext.RequestAborted);

        var plan = await planService.GetAsync(new PlanId(planRequestDto.PlanId), HttpContext.RequestAborted);

        return Ok(plan.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PlansWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPatch("{PlanId:guid}")]
    [ProducesResponseType<ResponseDataDto<PlanResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlan(UpdatePlanRequestDto updatePlanRequestDto)
    {
        await updatePlanRequestValidator.ValidateAndThrowAsync(updatePlanRequestDto, HttpContext.RequestAborted);

        var plan = await planService.UpdateAsync(updatePlanRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(plan.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PlansWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("{PlanId:guid}/versions")]
    [ProducesResponseType<ResponseDataDto<PlanVersionResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PublishPlanVersion(PublishPlanVersionRequestDto publishPlanVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        publishPlanVersionRequestDto.PublishedByUserId = authenticatedUser.UserId;
        await publishPlanVersionRequestValidator.ValidateAndThrowAsync(publishPlanVersionRequestDto, HttpContext.RequestAborted);

        var planVersion = await planService.PublishVersionAsync(publishPlanVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, planVersion.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.PlansRead)]
    [HttpGet("{PlanId:guid}/versions")]
    [ProducesResponseType<PagedResponseDto<PlanVersionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPlanVersions([FromQuery] ListPlanVersionRequestDto listPlanVersionRequestDto)
    {
        await listPlanVersionRequestValidator.ValidateAndThrowAsync(listPlanVersionRequestDto, HttpContext.RequestAborted);

        var planVersions = await planService.ListVersionsAsync(listPlanVersionRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(planVersions.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.PlansWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{PlanId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePlan(DeletePlanRequestDto deletePlanRequestDto)
    {
        await deletePlanRequestValidator.ValidateAndThrowAsync(deletePlanRequestDto, HttpContext.RequestAborted);

        await planService.DeleteAsync(new PlanId(deletePlanRequestDto.PlanId), HttpContext.RequestAborted);

        return NoContent();
    }
}
