using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.BillingEntities;
using Presentation.Mappers.Common;
using Presentation.Models.BillingEntities;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("billing-entities")]
public class BillingEntityController(
    IApplicationUserService applicationUserService,
    IBillingEntityService billingEntityService,
    IValidator<CreateBillingEntityRequestDto> createBillingEntityRequestValidator,
    IValidator<UpdateBillingEntityRequestDto> updateBillingEntityRequestValidator,
    IValidator<BillingEntityRequestDto> billingEntityRequestValidator,
    IValidator<DeleteBillingEntityRequestDto> deleteBillingEntityRequestValidator,
    IValidator<ListBillingEntityRequestDto> listBillingEntityRequestValidator) : Controller
{
    [RequirePermission(Permission.BillingEntitiesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<BillingEntityResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBillingEntity(CreateBillingEntityRequestDto createBillingEntityRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createBillingEntityRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createBillingEntityRequestValidator.ValidateAndThrowAsync(createBillingEntityRequestDto, HttpContext.RequestAborted);

        var billingEntity = await billingEntityService.CreateAsync(
            createBillingEntityRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, billingEntity.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.BillingEntitiesRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<BillingEntityResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBillingEntities([FromQuery] ListBillingEntityRequestDto listBillingEntityRequestDto)
    {
        await listBillingEntityRequestValidator.ValidateAndThrowAsync(listBillingEntityRequestDto, HttpContext.RequestAborted);

        var billingEntities = await billingEntityService.ListAsync(
            listBillingEntityRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(billingEntities.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.BillingEntitiesRead)]
    [HttpGet("{BillingEntityId:guid}")]
    [ProducesResponseType<ResponseDataDto<BillingEntityResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingEntity(BillingEntityRequestDto billingEntityRequestDto)
    {
        await billingEntityRequestValidator.ValidateAndThrowAsync(billingEntityRequestDto, HttpContext.RequestAborted);

        var billingEntity = await billingEntityService.GetAsync(
            new BillingEntityId(billingEntityRequestDto.BillingEntityId), HttpContext.RequestAborted);

        return Ok(billingEntity.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.BillingEntitiesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPatch("{BillingEntityId:guid}")]
    [ProducesResponseType<ResponseDataDto<BillingEntityResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBillingEntity(UpdateBillingEntityRequestDto updateBillingEntityRequestDto)
    {
        await updateBillingEntityRequestValidator.ValidateAndThrowAsync(updateBillingEntityRequestDto, HttpContext.RequestAborted);

        var billingEntity = await billingEntityService.UpdateAsync(
            updateBillingEntityRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(billingEntity.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.BillingEntitiesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{BillingEntityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBillingEntity(DeleteBillingEntityRequestDto deleteBillingEntityRequestDto)
    {
        await deleteBillingEntityRequestValidator.ValidateAndThrowAsync(deleteBillingEntityRequestDto, HttpContext.RequestAborted);

        await billingEntityService.DeleteAsync(
            new BillingEntityId(deleteBillingEntityRequestDto.BillingEntityId), HttpContext.RequestAborted);

        return NoContent();
    }
}
