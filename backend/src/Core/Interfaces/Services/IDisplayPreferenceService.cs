using Core.Localization;
using Core.Models.Localization;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IDisplayPreferenceService
{
    Task<DisplayPreferences> ForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken);

    Task<DisplayPreferences> ForOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    DisplayPreferences Resolve(Locale? statedLocale, DisplayTimeZone? statedTimeZone, Organization? organization);

    Task<DisplayPreferences> ForStatedAsync(Locale? statedLocale, OrganizationId organizationId, CancellationToken stoppingToken);

    Task<Translator> TranslatorForOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    DisplayPreferences Fallback();
}
