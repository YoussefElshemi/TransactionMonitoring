using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Mappers.Queues;
using Presentation.Models.Cases;
using Presentation.Models.Common;
using Presentation.Models.Queues;

namespace Presentation.Controllers;

[RequirePermission(Permission.QueuesClaim)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("queues")]
public class QueueController(
    IApplicationUserService applicationUserService,
    IQueueService queueService,
    IQueueRoutingService queueRoutingService,
    QueueResponseFactory queueResponseFactory,
    CaseResponseFactory caseResponseFactory,
    IValidator<ListMyQueueRequestDto> listMyQueueRequestValidator,
    IValidator<ClaimNextRequestDto> claimNextRequestValidator) : Controller
{
    [HttpGet("my")]
    [ProducesResponseType<PagedResponseDto<QueueResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyQueues([FromQuery] ListMyQueueRequestDto listMyQueueRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyQueueRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        listMyQueueRequestDto.MemberUserId = authenticatedUser.UserId;
        await listMyQueueRequestValidator.ValidateAndThrowAsync(listMyQueueRequestDto, HttpContext.RequestAborted);

        var queues = await queueService.ListAsync(listMyQueueRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await queueResponseFactory.CreateAsync(
            queues.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(queues.ToDto(resources));
    }

    [RequirePermission(Permission.QueuesClaim)]
    [Idempotent]
    [HttpPost("claim-next")]
    [ProducesResponseType<ResponseDataDto<CaseResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> ClaimNext()
    {
        return ClaimNextAsync(new ClaimNextRequestDto { QueueId = null });
    }

    [RequirePermission(Permission.QueuesClaim)]
    [Idempotent]
    [HttpPost("{QueueId:guid}/claim-next")]
    [ProducesResponseType<ResponseDataDto<CaseResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> ClaimNextFromQueue(ClaimNextRequestDto claimNextRequestDto)
    {
        return ClaimNextAsync(claimNextRequestDto);
    }

    private async Task<IActionResult> ClaimNextAsync(ClaimNextRequestDto claimNextRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        claimNextRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        claimNextRequestDto.UserId = authenticatedUser.UserId;
        await claimNextRequestValidator.ValidateAndThrowAsync(claimNextRequestDto, HttpContext.RequestAborted);

        var @case = await queueRoutingService.ClaimNextAsync(claimNextRequestDto.ToModel(), HttpContext.RequestAborted);

        if (@case is null)
        {
            return NoContent();
        }

        var resource = await caseResponseFactory.CreateAsync(
            @case, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }
}
