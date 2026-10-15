using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Providers;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Payments;
using Presentation.Models.Accounts;
using Presentation.Models.Common;
using Presentation.Models.Payments;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/payment-methods")]
public class OrganizationPaymentMethodController(
    IBillingProfileService billingProfileService,
    IPaymentProvider paymentProvider,
    IValidator<AccountRequestDto> accountRequestValidator,
    IValidator<CreatePaymentMethodSessionRequestDto> createPaymentMethodSessionRequestValidator) : Controller
{
    [RequirePermission(Permission.PaymentMethodsRead)]
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<PaymentMethodResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPaymentMethods(AccountRequestDto accountRequestDto)
    {
        await accountRequestValidator.ValidateAndThrowAsync(accountRequestDto, HttpContext.RequestAborted);

        var organizationId = new OrganizationId(accountRequestDto.OrganizationId);
        var billingProfile = await billingProfileService.FindAsync(organizationId, HttpContext.RequestAborted);

        if (billingProfile?.ProviderCustomerReference is null)
        {
            return Ok(Array.Empty<PaymentMethodResponseDto>().ToResponseData());
        }

        var paymentMethods = await paymentProvider.ListPaymentMethodsAsync(
            billingProfile.ProviderCustomerReference.Value, HttpContext.RequestAborted);

        return Ok(paymentMethods.ConvertAll(x => x.ToDto()).ToArray().ToResponseData());
    }

    [RequirePermission(Permission.PaymentMethodsWrite)]
    [IdempotencyExempt(
        "A setup session is a short-lived URL rather than a record. Replaying one would hand back a link that has since expired, and creating a second costs nothing.")]
    [HttpPost("sessions")]
    [ProducesResponseType<ResponseDataDto<PaymentMethodSessionResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePaymentMethodSession(
        CreatePaymentMethodSessionRequestDto createPaymentMethodSessionRequestDto)
    {
        await createPaymentMethodSessionRequestValidator.ValidateAndThrowAsync(
            createPaymentMethodSessionRequestDto, HttpContext.RequestAborted);

        var customerReference = await billingProfileService.EnsureProviderCustomerAsync(
            new OrganizationId(createPaymentMethodSessionRequestDto.OrganizationId), HttpContext.RequestAborted);

        var url = await paymentProvider.CreateSessionAsync(
            new ProviderSessionRequest
            {
                CustomerReference = customerReference,
                ReturnUrl = new HostedSessionUrl(createPaymentMethodSessionRequestDto.Body.ReturnUrl),
                CancelUrl = new HostedSessionUrl(createPaymentMethodSessionRequestDto.Body.CancelUrl)
            },
            HttpContext.RequestAborted);

        return StatusCode(
            StatusCodes.Status201Created,
            new PaymentMethodSessionResponseDto { Url = url }.ToResponseData());
    }
}
