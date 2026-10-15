using Core.Configs;
using Core.Models.Alerts;
using Core.Models.Organizations;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Helpers;

public static class AlertPolicyResolver
{
    public static AlertPolicy Resolve(Rule rule, Organization? organization, AlertConfig alertConfig)
    {
        return new AlertPolicy
        {
            SubjectRole = rule.AlertSubjectRole ?? alertConfig.DefaultSubjectRole,

            DeduplicationWindow = rule.DeduplicationWindow
                                  ?? organization?.DeduplicationWindow
                                  ?? new DeduplicationWindow(alertConfig.DefaultDeduplicationWindowMinutes),

            CaseGroupingPeriod = organization?.CaseGroupingPeriod
                                 ?? new CaseGroupingPeriod(alertConfig.DefaultCaseGroupingPeriodMinutes),

            CaseGroupingEnabled = organization?.CaseGroupingEnabled
                                  ?? new IsEnabled(alertConfig.DefaultCaseGroupingEnabled),

            DeduplicationEnabled = organization?.DeduplicationEnabled
                                   ?? new IsEnabled(alertConfig.DefaultDeduplicationEnabled),

            CaseGroupingScope = organization?.CaseGroupingScope
                                ?? alertConfig.DefaultCaseGroupingScope,

            CaseGroupingSeverityPolicy = organization?.CaseGroupingSeverityPolicy
                                         ?? alertConfig.DefaultCaseGroupingSeverityPolicy,

            MaxAlertsPerCase = organization?.MaxAlertsPerCase
        };
    }
}
