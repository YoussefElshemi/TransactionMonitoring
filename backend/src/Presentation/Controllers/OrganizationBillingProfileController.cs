using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.BillingProfiles;
using Presentation.Mappers.Common;
using Presentation.Models.BillingProfiles;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/billing-profile")]
public class OrganizationBillingProfileController(
    IApplicationUserService applicationUserService,
    IBillingProfileService billingProfileService,
    IValidator<SaveBillingProfileRequestDto> saveBillingProfileRequestValidator,
    IValidator<BillingProfileRequestDto> billingProfileRequestValidator,
    IValidator<VerifyTaxIdentifierRequestDto> verifyTaxIdentifierRequestValidator) : Controller
{
    [RequirePermission(Permission.BillingProfilesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType<ResponseDataDto<BillingProfileResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveBillingProfile(SaveBillingProfileRequestDto saveBillingProfileRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        saveBillingProfileRequestDto.UpdatedByUserId = authenticatedUser.UserId;
        await saveBillingProfileRequestValidator.ValidateAndThrowAsync(saveBillingProfileRequestDto, HttpContext.RequestAborted);

        var billingProfile = await billingProfileService.SaveAsync(
            saveBillingProfileRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(billingProfile.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.BillingProfilesRead)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<BillingProfileResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingProfile(BillingProfileRequestDto billingProfileRequestDto)
    {
        await billingProfileRequestValidator.ValidateAndThrowAsync(billingProfileRequestDto, HttpContext.RequestAborted);

        var billingProfile = await billingProfileService.GetAsync(
            new OrganizationId(billingProfileRequestDto.OrganizationId), HttpContext.RequestAborted);

        return Ok(billingProfile.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.BillingProfilesWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("verify-tax-identifier")]
    [ProducesResponseType<ResponseDataDto<BillingProfileResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyTaxIdentifier(VerifyTaxIdentifierRequestDto verifyTaxIdentifierRequestDto)
    {
        await verifyTaxIdentifierRequestValidator.ValidateAndThrowAsync(verifyTaxIdentifierRequestDto, HttpContext.RequestAborted);

        var billingProfile = await billingProfileService.VerifyTaxIdentifierAsync(
            new OrganizationId(verifyTaxIdentifierRequestDto.OrganizationId), HttpContext.RequestAborted);

        return Ok(billingProfile.ToDto().ToResponseData());
    }
}
