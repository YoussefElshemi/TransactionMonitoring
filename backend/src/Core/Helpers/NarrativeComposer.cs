using System.Globalization;
using System.Text;
using Core.Models.RuleEvaluations;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Helpers;

public static class NarrativeComposer
{
    private const string NotRecorded = "not recorded";

    private static readonly string TruncationNotice = Environment.NewLine + Environment.NewLine +
        "[This draft was truncated because the case holds more detail than a filing narrative can carry. " +
        "Read the case in full before filing.]";

    public static string Compose(NarrativeSource narrativeSource, DateTimeOffset composedAt)
    {
        var builder = new StringBuilder();

        AppendSummary(builder, narrativeSource, composedAt);
        AppendPayment(builder, narrativeSource);
        AppendRules(builder, narrativeSource);
        AppendParties(builder, narrativeSource);
        AppendNotes(builder, narrativeSource);

        var narrative = builder.ToString().TrimEnd();

        return narrative.Length <= Narrative.MaxLength ? narrative : Truncate(narrative);
    }

    private static string Truncate(string narrative)
    {
        return narrative[..(Narrative.MaxLength - TruncationNotice.Length)].TrimEnd() + TruncationNotice;
    }

    private static void AppendSummary(StringBuilder builder, NarrativeSource narrativeSource, DateTimeOffset composedAt)
    {
        var @case = narrativeSource.Case;

        builder.AppendLine("SUMMARY");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Case {@case.FriendlyName} was opened on {@case.CreatedAt:u} at {@case.Severity} severity.");

        if (@case.Disposition.HasValue)
        {
            var reasonCode = @case.ReasonCode is not null ? $" ({@case.ReasonCode.Code})" : string.Empty;

            builder.AppendLine(CultureInfo.InvariantCulture,
                $"It was closed as {@case.Disposition.Value}{reasonCode} on {(@case.ResolvedAt.HasValue ? @case.ResolvedAt.Value.ToString("u") : NotRecorded)}.");
        }
        else
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"The case remains open with a status of {@case.Status}.");
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"This narrative was drafted on {composedAt:u} and requires review before filing.");
        builder.AppendLine();
    }

    private static void AppendPayment(StringBuilder builder, NarrativeSource narrativeSource)
    {
        builder.AppendLine("PAYMENT");

        if (narrativeSource.Transaction is null)
        {
            builder.AppendLine("No single payment underlies this case; it was raised from grouped alert activity.");
            builder.AppendLine();

            return;
        }

        var transaction = narrativeSource.Transaction;
        var externalReference = transaction.ExternalReference.HasValue ? transaction.ExternalReference.Value.ToString() : NotRecorded;

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Payment {transaction.FriendlyName} (customer reference {externalReference}) was received on {transaction.CreatedAt:u}.");

        if (transaction.Decision is not null)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"The payment was {transaction.Decision.Status}.");
        }

        builder.AppendLine();
    }

    private static void AppendRules(StringBuilder builder, NarrativeSource narrativeSource)
    {
        builder.AppendLine("WHY IT WAS FLAGGED");

        var matched = narrativeSource.RuleEvaluations.Where(x => x.Matched).ToList();

        if (matched.Count == 0)
        {
            builder.AppendLine("No monitoring rule matched; this case was raised by hand.");
            builder.AppendLine();

            return;
        }

        foreach (var ruleEvaluation in matched)
        {
            var name = ruleEvaluation.Rule is not null ? ruleEvaluation.Rule.FriendlyName.ToString() : ruleEvaluation.RuleId.ToString();

            var version = ruleEvaluation.RuleVersion is not null
                ? $" (version {ruleEvaluation.RuleVersion.VersionNumber})"
                : string.Empty;

            builder.AppendLine(CultureInfo.InvariantCulture, $"- {name}{version}");

            foreach (var conditionMatch in (ruleEvaluation.ConditionMatches ?? []).Where(x => x.Matched))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    {DescribeCondition(conditionMatch)}");
            }
        }

        builder.AppendLine();
    }

    private static string DescribeCondition(ConditionMatch conditionMatch)
    {
        var condition = conditionMatch.Condition.HasValue ? conditionMatch.Condition.Value.ToString() : "matched";
        var expected = conditionMatch.Expected?.ToJsonString() ?? NotRecorded;
        var actual = conditionMatch.Actual?.ToJsonString() ?? NotRecorded;

        return $"{conditionMatch.Field} {condition} {expected}, observed {actual}";
    }

    private static void AppendParties(StringBuilder builder, NarrativeSource narrativeSource)
    {
        if (narrativeSource.Parties.Count == 0)
        {
            return;
        }

        builder.AppendLine("PARTIES");

        foreach (var history in narrativeSource.Parties)
        {
            var party = history.Party;
            var displayName = party.DisplayName.HasValue ? party.DisplayName.Value.ToString() : NotRecorded;

            builder.AppendLine(CultureInfo.InvariantCulture, $"- {displayName} ({party.Identifier})");
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"    first seen {party.FirstSeenAt:u}, last seen {party.LastSeenAt:u}, {history.TransactionCount} payments, {history.CaseCount} cases");

            if (party.RiskScore.HasValue)
            {
                var riskNote = party.RiskNote.HasValue ? $" — {party.RiskNote.Value}" : string.Empty;

                builder.AppendLine(CultureInfo.InvariantCulture, $"    risk scored {party.RiskScore.Value} by this desk{riskNote}");
            }
        }

        builder.AppendLine();
    }

    private static void AppendNotes(StringBuilder builder, NarrativeSource narrativeSource)
    {
        if (narrativeSource.Notes.Count == 0)
        {
            return;
        }

        builder.AppendLine("INVESTIGATION");

        foreach (var caseNote in narrativeSource.Notes)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {caseNote.CreatedAt:u}: {caseNote.Message}");
        }

        builder.AppendLine();
    }
}
