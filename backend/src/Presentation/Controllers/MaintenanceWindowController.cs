using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Status;
using Presentation.Models.Common;
using Presentation.Models.Status;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("maintenance-windows")]
public class MaintenanceWindowController(
    IApplicationUserService applicationUserService,
    IMaintenanceWindowService maintenanceWindowService,
    IValidator<CreateMaintenanceWindowRequestDto> createMaintenanceWindowRequestValidator,
    IValidator<MaintenanceWindowRequestDto> maintenanceWindowRequestValidator,
    IValidator<UpdateMaintenanceWindowRequestDto> updateMaintenanceWindowRequestValidator,
    IValidator<CancelMaintenanceWindowRequestDto> cancelMaintenanceWindowRequestValidator,
    IValidator<ListMaintenanceWindowRequestDto> listMaintenanceWindowRequestValidator) : Controller
{
    [RequirePermission(Permission.MaintenanceWindowsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<MaintenanceWindowResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMaintenanceWindow(CreateMaintenanceWindowRequestDto createMaintenanceWindowRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createMaintenanceWindowRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createMaintenanceWindowRequestValidator.ValidateAndThrowAsync(createMaintenanceWindowRequestDto, HttpContext.RequestAborted);

        var maintenanceWindow = await maintenanceWindowService.CreateAsync(
            createMaintenanceWindowRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, maintenanceWindow.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.MaintenanceWindowsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<MaintenanceWindowResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMaintenanceWindows([FromQuery] ListMaintenanceWindowRequestDto listMaintenanceWindowRequestDto)
    {
        await listMaintenanceWindowRequestValidator.ValidateAndThrowAsync(listMaintenanceWindowRequestDto, HttpContext.RequestAborted);

        var maintenanceWindows = await maintenanceWindowService.ListAsync(
            listMaintenanceWindowRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(maintenanceWindows.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.MaintenanceWindowsRead)]
    [HttpGet("{MaintenanceWindowId:guid}")]
    [ProducesResponseType<ResponseDataDto<MaintenanceWindowResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceWindow(MaintenanceWindowRequestDto maintenanceWindowRequestDto)
    {
        await maintenanceWindowRequestValidator.ValidateAndThrowAsync(maintenanceWindowRequestDto, HttpContext.RequestAborted);

        var maintenanceWindow = await maintenanceWindowService.GetAsync(
            new MaintenanceWindowId(maintenanceWindowRequestDto.MaintenanceWindowId), HttpContext.RequestAborted);

        return Ok(maintenanceWindow.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.MaintenanceWindowsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPut("{MaintenanceWindowId:guid}")]
    [ProducesResponseType<ResponseDataDto<MaintenanceWindowResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMaintenanceWindow(UpdateMaintenanceWindowRequestDto updateMaintenanceWindowRequestDto)
    {
        await updateMaintenanceWindowRequestValidator.ValidateAndThrowAsync(updateMaintenanceWindowRequestDto, HttpContext.RequestAborted);

        var maintenanceWindow = await maintenanceWindowService.UpdateAsync(
            updateMaintenanceWindowRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(maintenanceWindow.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.MaintenanceWindowsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{MaintenanceWindowId:guid}")]
    [ProducesResponseType<ResponseDataDto<MaintenanceWindowResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelMaintenanceWindow(CancelMaintenanceWindowRequestDto cancelMaintenanceWindowRequestDto)
    {
        await cancelMaintenanceWindowRequestValidator.ValidateAndThrowAsync(cancelMaintenanceWindowRequestDto, HttpContext.RequestAborted);

        var maintenanceWindow = await maintenanceWindowService.CancelAsync(
            new MaintenanceWindowId(cancelMaintenanceWindowRequestDto.MaintenanceWindowId), HttpContext.RequestAborted);

        return Ok(maintenanceWindow.ToDto().ToResponseData());
    }
}
