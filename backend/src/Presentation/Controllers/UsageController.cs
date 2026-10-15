using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Usages;
using Presentation.Models.Common;
using Presentation.Models.Usages;

namespace Presentation.Controllers;

[RequirePermission(Permission.UsagesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("usages")]
public class UsageController(
    IApplicationUserService applicationUserService,
    IUsageService usageService,
    IValidator<UsageRequestDto> usageRequestValidator,
    IValidator<ListUsageRequestDto> listUsageRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsages([FromQuery] ListUsageRequestDto listUsageRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listUsageRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listUsageRequestValidator.ValidateAndThrowAsync(listUsageRequestDto, HttpContext.RequestAborted);

        var usages = await usageService.ListAsync(listUsageRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(usages.ToDto(UsageDtoMapper.ToDto));
    }

    [HttpGet("{UsageId:guid}")]
    [ProducesResponseType<ResponseDataDto<UsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage(UsageRequestDto usageRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        usageRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await usageRequestValidator.ValidateAndThrowAsync(usageRequestDto, HttpContext.RequestAborted);

        var usage = await usageService.GetAsync(new UsageId(usageRequestDto.UsageId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }
}