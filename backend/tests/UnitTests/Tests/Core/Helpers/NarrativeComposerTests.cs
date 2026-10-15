using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.RuleEvaluations;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseNotes;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Parties;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

namespace UnitTests.Tests.Core.Helpers;

public class NarrativeComposerTests : BaseTestClass
{
    private static readonly DateTimeOffset ComposedAt = new(2026, 8, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compose_NamesTheCaseAndItsSeverity()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture) with { Severity = RuleSeverity.High, Disposition = null };
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Case = @case };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain(@case.FriendlyName);
        narrative.Should().Contain("High severity");
    }

    [Fact]
    public void Compose_WithAnOpenCase_SaysTheCaseIsStillOpen()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture) with { Status = CaseStatus.InReview, Disposition = null };
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Case = @case };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("remains open");
        narrative.Should().Contain(nameof(CaseStatus.InReview));
    }

    [Fact]
    public void Compose_WithAClosedCase_NamesTheDisposition()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture) with { Disposition = CaseDisposition.TruePositive };
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Case = @case };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain(nameof(CaseDisposition.TruePositive));
    }

    [Fact]
    public void Compose_AlwaysSaysTheDraftNeedsReview()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture);

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("requires review before filing");
    }

    [Fact]
    public void Compose_WithNoTransaction_SaysThereIsNoSinglePayment()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = null };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("No single payment underlies this case");
    }

    [Fact]
    public void Compose_WithATransaction_NamesThePaymentAndItsCustomerReference()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Transaction = transaction };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain(transaction.FriendlyName);
        narrative.Should().Contain(transaction.ExternalReference!.Value.ToString());
    }

    [Fact]
    public void Compose_WithAMatchedRule_NamesTheRuleAndItsVersion()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);

        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            Rule = rule,
            RuleVersion = ruleVersion,
            Matched = new Matched(true),
            ConditionMatches = []
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { RuleEvaluations = [ruleEvaluation] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain(rule.FriendlyName);
        narrative.Should().Contain($"version {ruleVersion.VersionNumber}");
    }

    [Fact]
    public void Compose_WithAMatchedRule_WritesTheExactValuesThatMatched()
    {
        // Arrange
        var conditionMatch = new ConditionMatch
        {
            Field = "amount",
            Condition = RuleCondition.GreaterThan,
            Expected = JsonValue.Create(10000),
            Actual = JsonValue.Create(24500),
            Matched = true
        };

        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            Matched = new Matched(true),
            ConditionMatches = [conditionMatch]
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { RuleEvaluations = [ruleEvaluation] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("amount GreaterThan 10000, observed 24500");
    }

    [Fact]
    public void Compose_WithAnUnmatchedEvaluation_LeavesItOutAndSaysNoRuleMatched()
    {
        // Arrange
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(false) };
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { RuleEvaluations = [ruleEvaluation] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("No monitoring rule matched");
        narrative.Should().NotContain(ruleEvaluation.Rule!.FriendlyName);
    }

    [Fact]
    public void Compose_WithAParty_WritesItsIdentifierAndHistoryCounts()
    {
        // Arrange
        var party = FakeParty.CreateValid(Fixture);

        var history = new NarrativePartyHistory
        {
            Party = party,
            CaseCount = new TotalCount(4),
            TransactionCount = new TotalCount(37)
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Parties = [history] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain(party.Identifier);
        narrative.Should().Contain("37 payments, 4 cases");
    }

    [Fact]
    public void Compose_WithARiskScoredParty_SaysThisDeskScoredThem()
    {
        // Arrange
        var party = FakeParty.CreateValid(Fixture) with
        {
            RiskScore = new RiskScore(85),
            RiskNote = new RiskNote("Repeated structuring")
        };

        var history = new NarrativePartyHistory
        {
            Party = party,
            CaseCount = new TotalCount(1),
            TransactionCount = new TotalCount(1)
        };

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Parties = [history] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("risk scored 85 by this desk");
        narrative.Should().Contain("Repeated structuring");
    }

    [Fact]
    public void Compose_WithCaseNotes_WritesThemUnderInvestigation()
    {
        // Arrange
        var caseNote = FakeCaseNote.CreateValid(Fixture) with { Message = new Message("Called the customer, no answer") };
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with { Notes = [caseNote] };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().Contain("INVESTIGATION");
        narrative.Should().Contain("Called the customer, no answer");
    }

    [Fact]
    public void Compose_WithNoPartiesOrNotes_OmitsThoseSections()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture);

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Should().NotContain("PARTIES");
        narrative.Should().NotContain("INVESTIGATION");
    }

    [Fact]
    public void Compose_FitsInsideWhatTheNarrativeValueObjectAccepts()
    {
        // Arrange
        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with
        {
            Transaction = FakeTransaction.CreateValid(Fixture),
            Notes = [FakeCaseNote.CreateValid(Fixture)],
            RuleEvaluations = [FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]
        };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        var act = () => new Narrative(narrative);

        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithTheNoteCapFullOfMaximumLengthNotes_StillFitsInsideTheNarrative()
    {
        // Arrange
        var notes = Enumerable.Range(0, 25)
            .Select(_ => FakeCaseNote.CreateValid(Fixture) with { Message = new Message(new string('x', 2048)) })
            .ToList();

        var narrativeSource = FakeNarrativeSource.CreateValid(Fixture) with
        {
            Transaction = FakeTransaction.CreateValid(Fixture),
            Notes = notes,
            RuleEvaluations = [FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]
        };

        // Act
        var narrative = NarrativeComposer.Compose(narrativeSource, ComposedAt);

        // Assert
        narrative.Length.Should().BeLessThanOrEqualTo(Narrative.MaxLength);
        narrative.Should().EndWith("Read the case in full before filing.]");

        var act = () => new Narrative(narrative);

        act.Should().NotThrow();
    }
}
