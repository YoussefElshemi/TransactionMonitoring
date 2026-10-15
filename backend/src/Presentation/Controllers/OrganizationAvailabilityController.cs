using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Availability;
using Presentation.Mappers.Common;
using Presentation.Models.Availability;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/availability")]
public class OrganizationAvailabilityController(
    IApplicationUserService applicationUserService,
    IAvailabilityService availabilityService,
    IAvailabilityCommitmentService availabilityCommitmentService,
    IValidator<MeasureAvailabilityRequestDto> measureAvailabilityRequestValidator,
    IValidator<CreateAvailabilityCommitmentRequestDto> createAvailabilityCommitmentRequestValidator,
    IValidator<AvailabilityCommitmentRequestDto> availabilityCommitmentRequestValidator,
    IValidator<DeleteAvailabilityCommitmentRequestDto> deleteAvailabilityCommitmentRequestValidator,
    IValidator<ListAvailabilityCommitmentRequestDto> listAvailabilityCommitmentRequestValidator) : Controller
{
    [RequirePermission(Permission.AvailabilityRead)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<AvailabilityMeasurementResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MeasureAvailability([FromQuery] MeasureAvailabilityRequestDto measureAvailabilityRequestDto)
    {
        await measureAvailabilityRequestValidator.ValidateAndThrowAsync(measureAvailabilityRequestDto, HttpContext.RequestAborted);

        var measurement = await availabilityService.MeasureAsync(
            measureAvailabilityRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(measurement.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.AvailabilityCommitmentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("commitments")]
    [ProducesResponseType<ResponseDataDto<AvailabilityCommitmentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAvailabilityCommitment(CreateAvailabilityCommitmentRequestDto createAvailabilityCommitmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createAvailabilityCommitmentRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createAvailabilityCommitmentRequestValidator.ValidateAndThrowAsync(createAvailabilityCommitmentRequestDto, HttpContext.RequestAborted);

        var availabilityCommitment = await availabilityCommitmentService.CreateAsync(
            createAvailabilityCommitmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, availabilityCommitment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.AvailabilityCommitmentsRead)]
    [HttpGet("commitments")]
    [ProducesResponseType<PagedResponseDto<AvailabilityCommitmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAvailabilityCommitments([FromQuery] ListAvailabilityCommitmentRequestDto listAvailabilityCommitmentRequestDto)
    {
        await listAvailabilityCommitmentRequestValidator.ValidateAndThrowAsync(listAvailabilityCommitmentRequestDto, HttpContext.RequestAborted);

        var availabilityCommitments = await availabilityCommitmentService.ListAsync(
            listAvailabilityCommitmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(availabilityCommitments.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.AvailabilityCommitmentsRead)]
    [HttpGet("commitments/{AvailabilityCommitmentId:guid}")]
    [ProducesResponseType<ResponseDataDto<AvailabilityCommitmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailabilityCommitment(AvailabilityCommitmentRequestDto availabilityCommitmentRequestDto)
    {
        await availabilityCommitmentRequestValidator.ValidateAndThrowAsync(availabilityCommitmentRequestDto, HttpContext.RequestAborted);

        var availabilityCommitment = await availabilityCommitmentService.GetAsync(
            new AvailabilityCommitmentId(availabilityCommitmentRequestDto.AvailabilityCommitmentId), HttpContext.RequestAborted);

        return Ok(availabilityCommitment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.AvailabilityCommitmentsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("commitments/{AvailabilityCommitmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAvailabilityCommitment(DeleteAvailabilityCommitmentRequestDto deleteAvailabilityCommitmentRequestDto)
    {
        await deleteAvailabilityCommitmentRequestValidator.ValidateAndThrowAsync(deleteAvailabilityCommitmentRequestDto, HttpContext.RequestAborted);

        await availabilityCommitmentService.DeleteAsync(
            new AvailabilityCommitmentId(deleteAvailabilityCommitmentRequestDto.AvailabilityCommitmentId), HttpContext.RequestAborted);

        return NoContent();
    }
}
