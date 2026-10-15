using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Statistics;
using Presentation.Models.Common;
using Presentation.Models.Statistics;

namespace Presentation.Controllers;

[RequirePermission(Permission.StatisticsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("statistics")]
public class StatisticsController(
    IApplicationUserService applicationUserService,
    IStatisticsService statisticsService,
    IValidator<StatisticRequestDto> statisticRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<StatisticResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetStatistics([FromQuery] StatisticRequestDto statisticRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        statisticRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await statisticRequestValidator.ValidateAndThrowAsync(statisticRequestDto, HttpContext.RequestAborted);

        var statistics = await statisticsService.GetStatisticsAsync(statisticRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(statistics.ToDto().ToResponseData());
    }
}