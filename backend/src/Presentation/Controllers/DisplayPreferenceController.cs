using System.Globalization;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Localization;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers;
using Presentation.Mappers.Common;
using Presentation.Models.Common;
using Presentation.Models.Localization;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("users/me/preferences")]
public class DisplayPreferenceController(
    IApplicationUserService applicationUserService,
    IDisplayPreferenceService displayPreferenceService,
    IValidator<UpdateDisplayPreferenceRequestDto> updateDisplayPreferenceRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<DisplayPreferenceResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences()
    {
        var user = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var resolved = displayPreferenceService.Resolve(user.Locale, user.TimeZone, user.Organization);

        return Ok(ToDto(user.Locale?.ToString(), user.TimeZone?.ToString(), resolved).ToResponseData());
    }

    [Idempotent]
    [HttpPut]
    [ProducesResponseType<ResponseDataDto<DisplayPreferenceResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateDisplayPreferenceRequestDto updateDisplayPreferenceRequestDto)
    {
        await updateDisplayPreferenceRequestValidator.ValidateAndThrowAsync(updateDisplayPreferenceRequestDto, HttpContext.RequestAborted);

        var user = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await applicationUserService.SetDisplayPreferencesAsync(
            user.UserId,
            updateDisplayPreferenceRequestDto.Locale.ToLocale(),
            updateDisplayPreferenceRequestDto.TimeZone.ToDisplayTimeZone(),
            HttpContext.RequestAborted);

        var updated = await applicationUserService.GetAsync(user.UserId, HttpContext.RequestAborted);

        var resolved = displayPreferenceService.Resolve(updated.Locale, updated.TimeZone, updated.Organization);

        return Ok(ToDto(updated.Locale?.ToString(), updated.TimeZone?.ToString(), resolved).ToResponseData());
    }

    [HttpGet("/localization/catalogue")]
    [ProducesResponseType<ResponseDataDto<LocalizationCatalogueResponseDto>>(StatusCodes.Status200OK)]
    public IActionResult GetCatalogue()
    {
        var fallback = displayPreferenceService.Fallback();

        var catalogue = new LocalizationCatalogueResponseDto
        {
            Languages = TranslationCatalogue.Supported.Select(x => x.ToString().ToLowerInvariant()).ToList(),

            // Specific cultures only: a neutral culture has no formatting conventions.
            Locales = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Order(StringComparer.Ordinal)
                .ToList(),

            // IANA ids regardless of host, so the picker matches what is stored across platforms.
            TimeZones = TimeZoneInfo.GetSystemTimeZones()
                .Select(DisplayTimeZone.Iana)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList(),
            DefaultLocale = fallback.Locale.ToString(),
            DefaultTimeZone = fallback.TimeZone.ToString()
        };

        return Ok(catalogue.ToResponseData());
    }

    private static DisplayPreferenceResponseDto ToDto(string? statedLocale, string? statedTimeZone, DisplayPreferences resolved)
    {
        return new DisplayPreferenceResponseDto
        {
            Locale = statedLocale,
            TimeZone = statedTimeZone,
            ResolvedLocale = resolved.Locale.ToString(),
            ResolvedTimeZone = resolved.TimeZone.ToString(),
            ResolvedLanguage = resolved.Language.ToString().ToLowerInvariant()
        };
    }
}
