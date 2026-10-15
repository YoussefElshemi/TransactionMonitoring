using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Core.Documents;
using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Parties;
using UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

namespace UnitTests.Tests.Core.Documents;

public class GoAmlReportDocumentTests : BaseTestClass
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 8, 22, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Write_CarriesTheOrganizationsReportingEntityId()
    {
        // Arrange
        var suspiciousActivityReport = FakeSuspiciousActivityReport.CreateValid(Fixture);
        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture);
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture);

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            suspiciousActivityReport, suspiciousActivityReportPolicy, narrativeSource, SubmittedAt));

        // Assert
        document.Element("rentity_id")!.Value.Should()
            .Be(suspiciousActivityReportPolicy.ReportingEntityId!.Value.ToString());
    }

    [Fact]
    public void Write_IdentifiesTheFilingByItsFriendlyName()
    {
        // Arrange
        var suspiciousActivityReport = FakeSuspiciousActivityReport.CreateValid(Fixture);

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            suspiciousActivityReport,
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            FakeNarrativeSource.CreateValid(Fixture),
            SubmittedAt));

        // Assert
        document.Element("entity_reference")!.Value.Should().Be(suspiciousActivityReport.FriendlyName);
    }

    [Fact]
    public void Write_PutsTheNarrativeInTheReasonElement()
    {
        // Arrange
        var suspiciousActivityReport = FakeSuspiciousActivityReport.CreateValid(Fixture) with
        {
            Narrative = new Narrative("Structuring across three days.")
        };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            suspiciousActivityReport,
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            FakeNarrativeSource.CreateValid(Fixture),
            SubmittedAt));

        // Assert
        document.Element("reason")!.Value.Should().Be("Structuring across three days.");
    }

    [Fact]
    public void Write_WithNoTransaction_OmitsTheTransactionElement()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = null };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture),
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            narrativeSource,
            SubmittedAt));

        // Assert
        document.Element("transaction").Should().BeNull();
    }

    [Fact]
    public void Write_WithADeclaredAmountPath_ReadsTheAmountOutOfThePayload()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            Payload = new JsonObject { ["amount"] = 24500.55m, ["currency"] = "GBP" }
        };

        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture) with
        {
            AmountPath = new PayloadFieldPath("amount"),
            CurrencyPath = new PayloadFieldPath("currency")
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = transaction };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture), suspiciousActivityReportPolicy, narrativeSource, SubmittedAt));

        // Assert
        var element = document.Element("transaction")!;

        element.Element("amount_local")!.Value.Should().Be("24500.55");
        element.Element("currency_code")!.Value.Should().Be("GBP");
    }

    [Fact]
    public void Write_WithNoDeclaredAmountPath_OmitsTheAmountRatherThanWritingZero()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with
        {
            Transaction = FakeTransaction.CreateValid(Fixture)
        };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture),
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            narrativeSource,
            SubmittedAt));

        // Assert
        document.Element("transaction")!.Element("amount_local").Should().BeNull();
    }

    [Fact]
    public void Write_WithAnAmountHeldAsAString_StillReadsIt()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            Payload = new JsonObject { ["amount"] = "1200.00" }
        };

        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture) with
        {
            AmountPath = new PayloadFieldPath("amount")
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = transaction };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture), suspiciousActivityReportPolicy, narrativeSource, SubmittedAt));

        // Assert
        document.Element("transaction")!.Element("amount_local")!.Value.Should().Be("1200.00");
    }

    [Fact]
    public void Write_WritesTheRemitterAndBeneficiaryIntoTheirOwnElements()
    {
        // Arrange
        var remitter = FakeParty.CreateValid(Fixture);
        var beneficiary = FakeParty.CreateValid(Fixture);

        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            TransactionParties =
            [
                TransactionParty(remitter, PartyRole.Remitter),
                TransactionParty(beneficiary, PartyRole.Beneficiary)
            ]
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = transaction };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture),
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            narrativeSource,
            SubmittedAt));

        // Assert
        var transactionElement = document.Element("transaction")!;

        transactionElement.Element("t_from")!.Descendants("account_number").Single().Value
            .Should().Be(remitter.Identifier);

        transactionElement.Element("t_to")!.Descendants("account_number").Single().Value
            .Should().Be(beneficiary.Identifier);

        transactionElement.Element("t_from")!.Element("from_person").Should().NotBeNull();
        transactionElement.Element("t_to")!.Element("to_person").Should().NotBeNull();
    }

    [Fact]
    public void Write_SendsOurOwnRiskScoreAsACommentRatherThanAStructuredField()
    {
        // Arrange
        var remitter = FakeParty.CreateValid(Fixture) with { RiskScore = new RiskScore(90) };

        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            TransactionParties = [TransactionParty(remitter, PartyRole.Remitter)]
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = transaction };

        // Act
        var document = Parse(GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture),
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            narrativeSource,
            SubmittedAt));

        // Assert
        document.Descendants("comments").Single().Value.Should().Be("Internal risk score 90.");
    }

    [Fact]
    public void Write_ProducesParseableXmlWithAReportRoot()
    {
        // Arrange
        // Act
        var content = GoAmlReportDocument.Write(
            FakeSuspiciousActivityReport.CreateValid(Fixture),
            FakeSuspiciousActivityReportPolicy.CreateValid(Fixture),
            FakeNarrativeSource.CreateValid(Fixture),
            SubmittedAt);

        // Assert
        var act = () => XDocument.Parse(Encoding.UTF8.GetString(content));

        act.Should().NotThrow();
        act().Root!.Name.LocalName.Should().Be("report");
    }

    private TransactionParty TransactionParty(Party party, PartyRole role)
    {
        return FakeTransactionParty.CreateValid(Fixture) with { PartyId = party.PartyId, Party = party, Role = role };
    }

    private static XElement Parse(byte[] content)
    {
        return XDocument.Parse(Encoding.UTF8.GetString(content)).Root!;
    }
}
