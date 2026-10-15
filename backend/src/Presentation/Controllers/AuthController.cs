using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Core.Constants;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Mappers.Common;
using Presentation.Mappers.Organizations;
using Presentation.Models.Auth;
using Presentation.Models.Common;
using Presentation.Models.Users;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<ApplicationUserEntity> userManager,
    IUserStore<ApplicationUserEntity> userStore,
    SignInManager<ApplicationUserEntity> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    IEmailSender<ApplicationUserEntity> emailSender,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : Controller
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("register")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequestDto, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies)
    {
        if (string.IsNullOrEmpty(registerRequestDto.Email) || !EmailAddressAttribute.IsValid(registerRequestDto.Email))
        {
            return ValidationProblem(
                CreateValidationProblem(userManager.ErrorDescriber.InvalidEmail(registerRequestDto.Email)));
        }

        if (string.IsNullOrEmpty(registerRequestDto.UserName))
        {
            return ValidationProblem(
                CreateValidationProblem(userManager.ErrorDescriber.InvalidUserName(registerRequestDto.UserName)));
        }

        var user = new ApplicationUserEntity
        {
            OrganizationId = SystemConstants.DefaultOrganization,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow(),
            IsDeleted = false,
            DeletedAt = null
        };

        var emailStore = (IUserEmailStore<ApplicationUserEntity>)userStore;
        await userStore.SetUserNameAsync(user, registerRequestDto.UserName, HttpContext.RequestAborted);
        await emailStore.SetEmailAsync(user, registerRequestDto.Email, HttpContext.RequestAborted);
        var result = await userManager.CreateAsync(user, registerRequestDto.Password);

        if (!result.Succeeded)
        {
            return ValidationProblem(CreateValidationProblem(result));
        }

        await userManager.AddToRoleAsync(user, UserRole.User.ToString());
        await SendConfirmationEmailAsync(user, HttpContext, registerRequestDto.Email);

        var useCookieScheme = useCookies == true || useSessionCookies == true;
        var isPersistent = useCookies == true && useSessionCookies != true;
        signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

        await signInManager.SignInAsync(user, isPersistent);

        return new EmptyResult();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies)
    {
        var useCookieScheme = useCookies == true || useSessionCookies == true;
        var isPersistent = useCookies == true && useSessionCookies != true;
        signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(loginRequestDto.UserName, loginRequestDto.Password, isPersistent, true);

        if (result.RequiresTwoFactor)
        {
            if (!string.IsNullOrEmpty(loginRequestDto.TwoFactorCode))
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(loginRequestDto.TwoFactorCode, isPersistent, isPersistent);
            }
            else if (!string.IsNullOrEmpty(loginRequestDto.TwoFactorRecoveryCode))
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(loginRequestDto.TwoFactorRecoveryCode);
            }
        }

        if (!result.Succeeded)
        {
            return Problem(
                result.ToString(),
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized"
            );
        }

        return new EmptyResult();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return new EmptyResult();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest refreshRequest)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc ||
            timeProvider.GetUtcNow() >= expiresUtc ||
            await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not { } user)
        {
            return Challenge();
        }

        var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
        return SignIn(newPrincipal, IdentityConstants.BearerScheme);
    }

    [HttpGet("confirmEmail")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code, [FromQuery] string? changedEmail)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return Unauthorized();
        }

        IdentityResult result;

        if (string.IsNullOrEmpty(changedEmail))
        {
            result = await userManager.ConfirmEmailAsync(user, code);
        }
        else
        {
            result = await userManager.ChangeEmailAsync(user, changedEmail, code);

            if (result.Succeeded)
            {
                result = await userManager.SetUserNameAsync(user, changedEmail);
            }
        }

        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Content("Thank you for confirming your email.");
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("resendConfirmationEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest resendRequest)
    {
        var user = await userManager.FindByEmailAsync(resendRequest.Email);
        if (user == null)
        {
            return Ok();
        }

        await SendConfirmationEmailAsync(user, HttpContext, resendRequest.Email);
        return Ok();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("forgotPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest resetRequest)
    {
        var user = await userManager.FindByEmailAsync(resetRequest.Email);

        if (user != null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            await emailSender.SendPasswordResetCodeAsync(user, resetRequest.Email, HtmlEncoder.Default.Encode(encodedToken));
        }

        return Ok();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("resetPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetRequest)
    {
        var user = await userManager.FindByEmailAsync(resetRequest.Email);

        if (user == null || !await userManager.IsEmailConfirmedAsync(user))
        {
            return ValidationProblem("Invalid token or user does not exist.", statusCode: StatusCodes.Status400BadRequest);
        }

        IdentityResult result;
        try
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetRequest.ResetCode));
            result = await userManager.ResetPasswordAsync(user, code, resetRequest.NewPassword);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
        }

        if (!result.Succeeded)
        {
            return ValidationProblem(CreateValidationProblem(result));
        }

        return Ok();
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("manage/2fa")]
    [Authorize]
    [ProducesResponseType<ResponseDataDto<TwoFactorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureTwoFactorAuthentication([FromBody] TwoFactorRequest tfaRequest)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (tfaRequest.Enable == true)
        {
            if (tfaRequest.ResetSharedKey)
            {
                return ValidationProblem("Resetting the 2FA shared key must disable 2FA until a valid token based on the new key is validated.");
            }

            if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
            {
                return ValidationProblem("A valid 2FA token is required to enable 2FA.");
            }

            if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
            {
                return ValidationProblem("The provided 2FA token is invalid.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
        else if (tfaRequest.Enable == false || tfaRequest.ResetSharedKey)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
        }

        if (tfaRequest.ResetSharedKey)
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
        }

        string[]? recoveryCodes = null;
        if (tfaRequest.ResetRecoveryCodes || (tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
        {
            var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            recoveryCodes = recoveryCodesEnumerable?.ToArray();
        }

        if (tfaRequest.ForgetMachine)
        {
            await signInManager.ForgetTwoFactorClientAsync();
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
            }
        }

        var response = new TwoFactorResponse
        {
            SharedKey = key,
            RecoveryCodes = recoveryCodes,
            RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
            IsTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
            IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user)
        };

        return Ok(response.ToResponseData());
    }

    [HttpGet("manage/info")]
    [Authorize]
    [ProducesResponseType<ResponseDataDto<UserResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserInfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var userInfoResponse = await CreateInfoResponseAsync(user, roles);

        return Ok(userInfoResponse.ToResponseData());
    }

    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("manage/info")]
    [Authorize]
    [ProducesResponseType<ResponseDataDto<UserResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserInfo([FromBody] InfoRequest infoRequest)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(infoRequest.NewEmail) && !EmailAddressAttribute.IsValid(infoRequest.NewEmail))
        {
            return ValidationProblem("Invalid email format.");
        }

        if (!string.IsNullOrEmpty(infoRequest.NewPassword))
        {
            if (string.IsNullOrEmpty(infoRequest.OldPassword))
            {
                return ValidationProblem("Old password is required to set a new password. If the old password is forgotten, use /resetPassword.");
            }

            var changePasswordResult = await userManager.ChangePasswordAsync(user, infoRequest.OldPassword, infoRequest.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                return ValidationProblem("Password change failed.");
            }
        }

        if (!string.IsNullOrEmpty(infoRequest.NewEmail))
        {
            if (await userManager.FindByEmailAsync(infoRequest.NewEmail) != null)
            {
                return ValidationProblem("Email is already in use.");
            }

            var email = await userManager.GetEmailAsync(user);
            if (email != infoRequest.NewEmail)
            {
                await SendConfirmationEmailAsync(user, HttpContext, infoRequest.NewEmail, true);
            }
        }

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var userInfoResponse = await CreateInfoResponseAsync(user, roles);

        return Ok(userInfoResponse.ToResponseData());
    }

    private async Task<UserResourceDto> CreateInfoResponseAsync(ApplicationUserEntity user, string[] roles)
    {
        var organization = await organizationService.GetAsync(new OrganizationId(user.OrganizationId), HttpContext.RequestAborted);

        return new UserResourceDto
        {
            User = new UserResponseDto
            {
                UserId = user.Id,
                Roles = roles,
                IsEnabled = user.OrganizationId != SystemConstants.DefaultOrganization,
                OrganizationId = user.OrganizationId,
                SubOrganizationId = user.SubOrganizationId,
                Email = await userManager.GetEmailAsync(user),
                UserName = await userManager.GetUserNameAsync(user),
                IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
                Locale = user.Locale,
                TimeZone = user.TimeZone,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            },
            Organization = organization.ToDto()
        };
    }

    private static ValidationProblemDetails CreateValidationProblem(IdentityResult result)
    {
        var problemDetails = new ValidationProblemDetails();
        foreach (var error in result.Errors)
        {
            problemDetails.Errors.Add(error.Code, [error.Description]);
        }

        return problemDetails;
    }

    private static ValidationProblemDetails CreateValidationProblem(IdentityError error)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Title = "Validation Error",
            Detail = error.Description
        };

        problemDetails.Errors.Add(error.Code, [error.Description]);
        return problemDetails;
    }

    private async Task SendConfirmationEmailAsync(ApplicationUserEntity user, HttpContext context, string email, bool isChange = false)
    {
        var code = isChange
            ? await userManager.GenerateChangeEmailTokenAsync(user, email)
            : await userManager.GenerateEmailConfirmationTokenAsync(user);

        if (isChange)
        {
            await userManager.ChangeEmailAsync(user, email, code);
        }
        else
        {
            await userManager.ConfirmEmailAsync(user, code);
        }
    }
}