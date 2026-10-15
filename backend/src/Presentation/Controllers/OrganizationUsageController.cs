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
[Route("organizations/{OrganizationId:guid}/usages")]
public class OrganizationUsageController(
    IUsageService usageService,
    IValidator<OrganizationListUsageRequestDto> organizationListUsageRequestValidator,
    IValidator<OrganizationUsageRequestDto> organizationUsageRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsages([FromQuery] OrganizationListUsageRequestDto organizationListUsageRequestDto)
    {
        await organizationListUsageRequestValidator.ValidateAndThrowAsync(
            organizationListUsageRequestDto, HttpContext.RequestAborted);

        var usages = await usageService.ListAsync(organizationListUsageRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(usages.ToDto(UsageDtoMapper.ToDto));
    }

    [HttpGet("{UsageId:guid}")]
    [ProducesResponseType<ResponseDataDto<UsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage(OrganizationUsageRequestDto organizationUsageRequestDto)
    {
        await organizationUsageRequestValidator.ValidateAndThrowAsync(
            organizationUsageRequestDto, HttpContext.RequestAborted);

        var usage = await usageService.GetAsync(new UsageId(organizationUsageRequestDto.UsageId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }
}
