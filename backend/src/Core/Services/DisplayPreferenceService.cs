using Core.Configs;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Localization;
using Core.Models.Organizations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class DisplayPreferenceService(
    IApplicationUserRepository applicationUserRepository,
    IOrganizationRepository organizationRepository,
    IOptions<AppConfig> appConfig) : IDisplayPreferenceService
{
    public async Task<DisplayPreferences> ForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var user = await applicationUserRepository.GetByIdAsync(userId, stoppingToken);

        return Resolve(user.Locale, user.TimeZone, user.Organization);
    }

    public async Task<DisplayPreferences> ForOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organization = await organizationRepository.GetAsync(organizationId, stoppingToken);

        return Resolve(null, null, organization);
    }

    public DisplayPreferences Resolve(Locale? statedLocale, DisplayTimeZone? statedTimeZone, Organization? organization)
    {
        return DisplayPreferenceResolver.Resolve(
            statedLocale,
            statedTimeZone,
            organization?.DefaultLocale,
            organization?.DefaultTimeZone,
            appConfig.Value.LocalizationConfig);
    }

    public async Task<DisplayPreferences> ForStatedAsync(
        Locale? statedLocale, OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organization = await organizationRepository.GetAsync(organizationId, stoppingToken);

        return Resolve(statedLocale, null, organization);
    }

    public async Task<Translator> TranslatorForOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return new Translator(await ForOrganizationAsync(organizationId, stoppingToken));
    }

    public DisplayPreferences Fallback()
    {
        return Resolve(null, null, null);
    }
}
