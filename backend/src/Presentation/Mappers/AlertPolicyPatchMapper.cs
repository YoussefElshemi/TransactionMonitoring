using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Presentation.Mappers;

public static class AlertPolicyPatchMapper
{
    public static Patch<DeduplicationWindow?> ToDeduplicationWindow(this Patch<int?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<DeduplicationWindow?>.Set(patch.Value.HasValue ? new DeduplicationWindow(patch.Value.Value) : null);
    }

    public static Patch<CaseGroupingPeriod?> ToCaseGroupingPeriod(this Patch<int?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<CaseGroupingPeriod?>.Set(patch.Value.HasValue ? new CaseGroupingPeriod(patch.Value.Value) : null);
    }

    public static Patch<MaxAlertsPerCase?> ToMaxAlertsPerCase(this Patch<int?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<MaxAlertsPerCase?>.Set(patch.Value.HasValue ? new MaxAlertsPerCase(patch.Value.Value) : null);
    }

    public static Patch<IsEnabled?> ToIsEnabled(this Patch<bool?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<IsEnabled?>.Set(patch.Value.HasValue ? new IsEnabled(patch.Value.Value) : null);
    }

    public static Patch<PartyRole?> ToAlertSubjectRole(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<PartyRole?>.Set(patch.Value is null ? null : Enum.Parse<PartyRole>(patch.Value, true));
    }

    public static Patch<CaseGroupingScope?> ToCaseGroupingScope(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<CaseGroupingScope?>.Set(patch.Value is null ? null : Enum.Parse<CaseGroupingScope>(patch.Value, true));
    }

    public static Patch<CaseGroupingSeverityPolicy?> ToCaseGroupingSeverityPolicy(this Patch<string?> patch)
    {
        if (!patch.IsSet)
        {
            return default;
        }

        return Patch<CaseGroupingSeverityPolicy?>.Set(patch.Value is null ? null : Enum.Parse<CaseGroupingSeverityPolicy>(patch.Value, true));
    }
}
