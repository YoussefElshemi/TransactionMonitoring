using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Commitments;
using Presentation.Mappers.Common;
using Presentation.Models.Commitments;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/commitments")]
public class OrganizationCommitmentController(
    IApplicationUserService applicationUserService,
    ICommitmentService commitmentService,
    IValidator<CreateCommitmentRequestDto> createCommitmentRequestValidator,
    IValidator<CommitmentRequestDto> commitmentRequestValidator,
    IValidator<DeleteCommitmentRequestDto> deleteCommitmentRequestValidator,
    IValidator<ListCommitmentRequestDto> listCommitmentRequestValidator) : Controller
{
    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<CommitmentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCommitment(CreateCommitmentRequestDto createCommitmentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createCommitmentRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createCommitmentRequestValidator.ValidateAndThrowAsync(createCommitmentRequestDto, HttpContext.RequestAborted);

        var commitment = await commitmentService.CreateAsync(
            createCommitmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, commitment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<CommitmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCommitments([FromQuery] ListCommitmentRequestDto listCommitmentRequestDto)
    {
        await listCommitmentRequestValidator.ValidateAndThrowAsync(listCommitmentRequestDto, HttpContext.RequestAborted);

        var commitments = await commitmentService.ListAsync(
            listCommitmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(commitments.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.ContractTermsRead)]
    [HttpGet("{CommitmentId:guid}")]
    [ProducesResponseType<ResponseDataDto<CommitmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommitment(CommitmentRequestDto commitmentRequestDto)
    {
        await commitmentRequestValidator.ValidateAndThrowAsync(commitmentRequestDto, HttpContext.RequestAborted);

        var commitment = await commitmentService.GetAsync(
            new CommitmentId(commitmentRequestDto.CommitmentId), HttpContext.RequestAborted);

        return Ok(commitment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.ContractTermsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{CommitmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCommitment(DeleteCommitmentRequestDto deleteCommitmentRequestDto)
    {
        await deleteCommitmentRequestValidator.ValidateAndThrowAsync(deleteCommitmentRequestDto, HttpContext.RequestAborted);

        await commitmentService.DeleteAsync(
            new CommitmentId(deleteCommitmentRequestDto.CommitmentId), HttpContext.RequestAborted);

        return NoContent();
    }
}
