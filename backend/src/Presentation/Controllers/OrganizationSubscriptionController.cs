using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Subscriptions;
using Presentation.Models.Common;
using Presentation.Models.Subscriptions;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/subscriptions")]
public class OrganizationSubscriptionController(
    IApplicationUserService applicationUserService,
    ISubscriptionService subscriptionService,
    IValidator<CreateSubscriptionRequestDto> createSubscriptionRequestValidator,
    IValidator<OrganizationSubscriptionRequestDto> organizationSubscriptionRequestValidator,
    IValidator<DeleteSubscriptionRequestDto> deleteSubscriptionRequestValidator,
    IValidator<ListSubscriptionRequestDto> listSubscriptionRequestValidator) : Controller
{
    [RequirePermission(Permission.SubscriptionsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<SubscriptionResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubscription(CreateSubscriptionRequestDto createSubscriptionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createSubscriptionRequestDto.CreatedByUserId = authenticatedUser.UserId;
        await createSubscriptionRequestValidator.ValidateAndThrowAsync(createSubscriptionRequestDto, HttpContext.RequestAborted);

        var subscription = await subscriptionService.CreateAsync(
            createSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, subscription.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SubscriptionsRead)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<SubscriptionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubscriptions([FromQuery] ListSubscriptionRequestDto listSubscriptionRequestDto)
    {
        await listSubscriptionRequestValidator.ValidateAndThrowAsync(listSubscriptionRequestDto, HttpContext.RequestAborted);

        var subscriptions = await subscriptionService.ListAsync(
            listSubscriptionRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(subscriptions.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.SubscriptionsRead)]
    [HttpGet("{SubscriptionId:guid}")]
    [ProducesResponseType<ResponseDataDto<SubscriptionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscription(OrganizationSubscriptionRequestDto organizationSubscriptionRequestDto)
    {
        await organizationSubscriptionRequestValidator.ValidateAndThrowAsync(organizationSubscriptionRequestDto, HttpContext.RequestAborted);

        var subscription = await subscriptionService.GetAsync(
            new SubscriptionId(organizationSubscriptionRequestDto.SubscriptionId), HttpContext.RequestAborted);

        return Ok(subscription.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SubscriptionsWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{SubscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubscription(DeleteSubscriptionRequestDto deleteSubscriptionRequestDto)
    {
        await deleteSubscriptionRequestValidator.ValidateAndThrowAsync(deleteSubscriptionRequestDto, HttpContext.RequestAborted);

        await subscriptionService.DeleteAsync(
            new SubscriptionId(deleteSubscriptionRequestDto.SubscriptionId), HttpContext.RequestAborted);

        return NoContent();
    }
}
