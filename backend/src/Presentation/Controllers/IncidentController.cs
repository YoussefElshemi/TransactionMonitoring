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
[Route("incidents")]
public class IncidentController(
    IApplicationUserService applicationUserService,
    IIncidentService incidentService,
    IValidator<CreateIncidentRequestDto> createIncidentRequestValidator,
    IValidator<IncidentRequestDto> incidentRequestValidator,
    IValidator<UpdateIncidentRequestDto> updateIncidentRequestValidator,
    IValidator<PostIncidentUpdateRequestDto> postIncidentUpdateRequestValidator,
    IValidator<PublishIncidentReviewRequestDto> publishIncidentReviewRequestValidator,
    IValidator<ListIncidentRequestDto> listIncidentRequestValidator) : Controller
{
    [RequirePermission(Permission.IncidentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<IncidentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateIncident(CreateIncidentRequestDto createIncidentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createIncidentRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createIncidentRequestValidator.ValidateAndThrowAsync(createIncidentRequestDto, HttpContext.RequestAborted);

        var incident = await incidentService.CreateAsync(
            createIncidentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, incident.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.IncidentsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<IncidentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIncidents([FromQuery] ListIncidentRequestDto listIncidentRequestDto)
    {
        await listIncidentRequestValidator.ValidateAndThrowAsync(listIncidentRequestDto, HttpContext.RequestAborted);

        var incidents = await incidentService.ListAsync(listIncidentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(incidents.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.IncidentsRead)]
    [HttpGet("{IncidentId:guid}")]
    [ProducesResponseType<ResponseDataDto<IncidentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncident(IncidentRequestDto incidentRequestDto)
    {
        await incidentRequestValidator.ValidateAndThrowAsync(incidentRequestDto, HttpContext.RequestAborted);

        var incident = await incidentService.GetAsync(
            new IncidentId(incidentRequestDto.IncidentId), HttpContext.RequestAborted);

        return Ok(incident.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.IncidentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPut("{IncidentId:guid}")]
    [ProducesResponseType<ResponseDataDto<IncidentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateIncident(UpdateIncidentRequestDto updateIncidentRequestDto)
    {
        await updateIncidentRequestValidator.ValidateAndThrowAsync(updateIncidentRequestDto, HttpContext.RequestAborted);

        var incident = await incidentService.UpdateAsync(
            updateIncidentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(incident.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.IncidentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("{IncidentId:guid}/updates")]
    [ProducesResponseType<ResponseDataDto<IncidentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PostIncidentUpdate(PostIncidentUpdateRequestDto postIncidentUpdateRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        postIncidentUpdateRequestDto.PostedByUserId = authenticatedUser.UserId;
        await postIncidentUpdateRequestValidator.ValidateAndThrowAsync(postIncidentUpdateRequestDto, HttpContext.RequestAborted);

        var incident = await incidentService.PostUpdateAsync(
            postIncidentUpdateRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, incident.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.IncidentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPut("{IncidentId:guid}/review")]
    [ProducesResponseType<ResponseDataDto<IncidentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishIncidentReview(PublishIncidentReviewRequestDto publishIncidentReviewRequestDto)
    {
        await publishIncidentReviewRequestValidator.ValidateAndThrowAsync(publishIncidentReviewRequestDto, HttpContext.RequestAborted);

        var incident = await incidentService.PublishReviewAsync(
            publishIncidentReviewRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(incident.ToDto().ToResponseData());
    }
}
