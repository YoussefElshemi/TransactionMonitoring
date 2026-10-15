using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using Core.Enums;
using Core.Helpers;
using Core.Models.Parties;
using Core.Models.SuspiciousActivityReports;

namespace Core.Documents;

public static class GoAmlReportDocument
{
    private const string SubmissionCode = "E";
    private const string ReportCode = "STR";

    public static byte[] Write(
        SuspiciousActivityReport suspiciousActivityReport,
        SuspiciousActivityReportPolicy suspiciousActivityReportPolicy,
        NarrativeSource narrativeSource,
        DateTimeOffset submittedAt)
    {
        var report = new XElement("report",
            new XElement("rentity_id", suspiciousActivityReportPolicy.ReportingEntityId!.Value.ToString()),
            new XElement("submission_code", SubmissionCode),
            new XElement("report_code", ReportCode),

            new XElement("entity_reference", suspiciousActivityReport.FriendlyName.ToString()),
            new XElement("submission_date", Format(submittedAt)),
            new XElement("reason", suspiciousActivityReport.Narrative.ToString()),
            new XElement("action", DescribeAction(narrativeSource)));

        var transaction = WriteTransaction(suspiciousActivityReportPolicy, narrativeSource);

        if (transaction is not null)
        {
            report.Add(transaction);
        }

        report.Add(WriteIndicators(narrativeSource));

        return Serialise(new XDocument(new XDeclaration("1.0", "utf-8", null), report));
    }

    private static string DescribeAction(NarrativeSource narrativeSource)
    {
        var decision = narrativeSource.Transaction?.Decision;

        if (decision is null)
        {
            return "No payment decision was recorded against this report.";
        }

        var resolvedAt = decision.ResolvedAt.HasValue ? Format(decision.ResolvedAt.Value) : Format(decision.CreatedAt);

        return $"The payment was {decision.Status} on {resolvedAt}.";
    }

    private static XElement? WriteTransaction(
        SuspiciousActivityReportPolicy suspiciousActivityReportPolicy,
        NarrativeSource narrativeSource)
    {
        var transaction = narrativeSource.Transaction;

        if (transaction is null)
        {
            return null;
        }

        var element = new XElement("transaction",
            new XElement("transactionnumber", transaction.FriendlyName.ToString()),
            new XElement("date_transaction", Format(transaction.CreatedAt)));

        var amount = ReadAmount(suspiciousActivityReportPolicy, transaction.Payload);

        if (amount.HasValue)
        {
            element.Add(new XElement("amount_local", amount.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var currency = ReadCurrency(suspiciousActivityReportPolicy, transaction.Payload);

        if (!string.IsNullOrWhiteSpace(currency))
        {
            element.Add(new XElement("currency_code", currency));
        }

        var remitter = FindParty(transaction.TransactionParties, PartyRole.Remitter);
        var beneficiary = FindParty(transaction.TransactionParties, PartyRole.Beneficiary);

        if (remitter is not null)
        {
            element.Add(new XElement("t_from", WriteParty(remitter, "from_person")));
        }

        if (beneficiary is not null)
        {
            element.Add(new XElement("t_to", WriteParty(beneficiary, "to_person")));
        }

        return element;
    }

    private static XElement WriteParty(Party party, string elementName)
    {
        var element = new XElement(elementName,
            new XElement("account_number", party.Identifier.ToString()));

        if (party.DisplayName.HasValue)
        {
            element.Add(new XElement("name", party.DisplayName.Value.ToString()));
        }

        if (party.RiskScore.HasValue)
        {
            element.Add(new XElement("comments", $"Internal risk score {party.RiskScore.Value}."));
        }

        return element;
    }

    private static XElement WriteIndicators(NarrativeSource narrativeSource)
    {
        var indicators = new XElement("report_indicators");

        foreach (var ruleEvaluation in narrativeSource.RuleEvaluations.Where(x => x.Matched))
        {
            var name = ruleEvaluation.Rule is not null
                ? ruleEvaluation.Rule.FriendlyName.ToString()
                : ruleEvaluation.RuleId.ToString();

            indicators.Add(new XElement("indicator", name));
        }

        return indicators;
    }

    private static Party? FindParty(List<TransactionParty> transactionParties, PartyRole role)
    {
        return transactionParties.FirstOrDefault(x => x.Role == role)?.Party;
    }

    private static decimal? ReadAmount(SuspiciousActivityReportPolicy suspiciousActivityReportPolicy, JsonObject payload)
    {
        if (suspiciousActivityReportPolicy.AmountPath is null)
        {
            return null;
        }

        var node = PayloadPath.ResolveSingle(payload, suspiciousActivityReportPolicy.AmountPath.Value);

        if (node is null)
        {
            return null;
        }

        return decimal.TryParse(Unquote(node), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ? amount : null;
    }

    private static string? ReadCurrency(SuspiciousActivityReportPolicy suspiciousActivityReportPolicy, JsonObject payload)
    {
        if (suspiciousActivityReportPolicy.CurrencyPath is null)
        {
            return null;
        }

        var node = PayloadPath.ResolveSingle(payload, suspiciousActivityReportPolicy.CurrencyPath.Value);

        return node is not null ? Unquote(node) : null;
    }

    // Payloads may carry the amount as a JSON number or a string.
    private static string Unquote(JsonNode node)
    {
        return node.ToJsonString().Trim('"');
    }

    private static string Format(DateTimeOffset timestamp)
    {
        return timestamp.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static byte[] Serialise(XDocument document)
    {
        using var stream = new MemoryStream();

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
