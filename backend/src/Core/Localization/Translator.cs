using Core.Constants;
using Core.Enums;
using Core.Models.Localization;
using Core.ValueObjects;

namespace Core.Localization;

public readonly record struct Translator
{
    public Translator(DisplayPreferences displayPreferences)
    {
        DisplayPreferences = displayPreferences;
    }

    public DisplayPreferences DisplayPreferences { get; }
    public Language Language => DisplayPreferences.Language;

    public string Text(string key)
    {
        return TranslationCatalogue.Find(Language, key);
    }

    public string Text(string key, params object?[] arguments)
    {
        return string.Format(DisplayPreferences.Locale.Culture(), TranslationCatalogue.Find(Language, key), arguments);
    }

    public string Moment(DateTimeOffset instant)
    {
        var timeZone = DisplayPreferences.TimeZone.Resolve();
        var local = TimeZoneInfo.ConvertTime(instant, timeZone);

        return local.ToString("g", DisplayPreferences.Locale.Culture()) + " (" + timeZone.Id + ")";
    }

    public string Severity(RuleSeverity ruleSeverity)
    {
        return Text(ruleSeverity switch
        {
            RuleSeverity.Info => TranslationKeys.SeverityInfo,
            RuleSeverity.Low => TranslationKeys.SeverityLow,
            RuleSeverity.Medium => TranslationKeys.SeverityMedium,
            RuleSeverity.High => TranslationKeys.SeverityHigh,
            RuleSeverity.Critical => TranslationKeys.SeverityCritical,
            _ => TranslationKeys.SeverityInfo
        });
    }

    public string FilingStatus(SuspiciousActivityReportStatus status)
    {
        return Text(status switch
        {
            SuspiciousActivityReportStatus.Draft => TranslationKeys.FilingStatusDraft,
            SuspiciousActivityReportStatus.PendingApproval => TranslationKeys.FilingStatusPendingApproval,
            SuspiciousActivityReportStatus.Approved => TranslationKeys.FilingStatusApproved,
            SuspiciousActivityReportStatus.Filed => TranslationKeys.FilingStatusFiled,
            SuspiciousActivityReportStatus.Acknowledged => TranslationKeys.FilingStatusAcknowledged,
            SuspiciousActivityReportStatus.NotFiled => TranslationKeys.FilingStatusNotFiled,
            _ => TranslationKeys.FilingStatusDraft
        });
    }

    public string IncidentStatus(IncidentStatus incidentStatus)
    {
        return Text(incidentStatus switch
        {
            Enums.IncidentStatus.Investigating => TranslationKeys.IncidentStatusInvestigating,
            Enums.IncidentStatus.Identified => TranslationKeys.IncidentStatusIdentified,
            Enums.IncidentStatus.Monitoring => TranslationKeys.IncidentStatusMonitoring,
            Enums.IncidentStatus.Resolved => TranslationKeys.IncidentStatusResolved,
            _ => TranslationKeys.IncidentStatusInvestigating
        });
    }

    public string IncidentImpact(IncidentImpact incidentImpact)
    {
        return Text(incidentImpact switch
        {
            Enums.IncidentImpact.None => TranslationKeys.IncidentImpactNone,
            Enums.IncidentImpact.Minor => TranslationKeys.IncidentImpactMinor,
            Enums.IncidentImpact.Major => TranslationKeys.IncidentImpactMajor,
            Enums.IncidentImpact.Critical => TranslationKeys.IncidentImpactCritical,
            _ => TranslationKeys.IncidentImpactNone
        });
    }

    public string TaxTreatment(TaxTreatment taxTreatment)
    {
        return Text(taxTreatment switch
        {
            Enums.TaxTreatment.Standard => TranslationKeys.TaxTreatmentStandard,
            Enums.TaxTreatment.ReverseCharge => TranslationKeys.TaxTreatmentReverseCharge,
            Enums.TaxTreatment.OutOfScope => TranslationKeys.TaxTreatmentOutOfScope,
            _ => TranslationKeys.TaxTreatmentStandard
        });
    }

    public string BillableResource(BillableResource? billableResource)
    {
        return Text(billableResource switch
        {
            Enums.BillableResource.RuleEvaluation => TranslationKeys.LineResourceRuleEvaluation,
            Enums.BillableResource.RuleSetEvaluation => TranslationKeys.LineResourceRuleSetEvaluation,
            Enums.BillableResource.RuleTest => TranslationKeys.LineResourceRuleTest,
            Enums.BillableResource.Backtest => TranslationKeys.LineResourceBacktest,
            Enums.BillableResource.SandboxWrite => TranslationKeys.LineResourceSandboxWrite,
            _ => TranslationKeys.LineResourceUnclassified
        });
    }

    public string Date(DateTimeOffset instant)
    {
        return instant.ToString("d", DisplayPreferences.Locale.Culture());
    }

    public string Number(decimal value, string format)
    {
        return value.ToString(format, DisplayPreferences.Locale.Culture());
    }

    public static Translator Fallback()
    {
        return new Translator(new DisplayPreferences
        {
            Locale = new Locale(LocalizationConstants.FallbackLocale),
            TimeZone = new DisplayTimeZone(LocalizationConstants.FallbackTimeZone)
        });
    }
}
