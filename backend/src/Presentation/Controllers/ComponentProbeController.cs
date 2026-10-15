using Core.Enums;
using Core.Interfaces.Services;
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
[Route("status/probes")]
public class ComponentProbeController(
    IStatusService statusService,
    IValidator<RecordComponentProbeRequestDto> recordComponentProbeRequestValidator) : Controller
{
    [RequirePermission(Permission.StatusProbesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [IdempotencyExempt(
        "A probe is an observation at an instant, not a command. Two identical posts are two observations, and collapsing them would lose a minute of an outage.")]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<ComponentProbeResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordProbe(RecordComponentProbeRequestDto recordComponentProbeRequestDto)
    {
        await recordComponentProbeRequestValidator.ValidateAndThrowAsync(recordComponentProbeRequestDto, HttpContext.RequestAborted);

        var componentProbe = await statusService.RecordProbeAsync(
            recordComponentProbeRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, componentProbe.ToDto().ToResponseData());
    }
}
