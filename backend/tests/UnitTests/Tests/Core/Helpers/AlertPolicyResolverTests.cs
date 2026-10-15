using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Organizations;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Helpers;

public class AlertPolicyResolverTests : BaseTestClass
{
    private static readonly AlertConfig AlertConfig = new()
    {
        DefaultDeduplicationWindowMinutes = 60,
        DefaultCaseGroupingPeriodMinutes = 1440,
        DefaultSubjectRole = PartyRole.Remitter
    };

    [Fact]
    public void Resolve_NoRuleOrOrganizationPolicy_FallsBackToConfiguredDefaults()
    {
        // Arrange
        var rule = Rule(null, null);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, null, AlertConfig);

        // Assert
        alertPolicy.SubjectRole.Should().Be(PartyRole.Remitter);
        ((int)alertPolicy.DeduplicationWindow).Should().Be(60);
        ((int)alertPolicy.CaseGroupingPeriod).Should().Be(1440);
    }

    [Fact]
    public void Resolve_NoRuleDeduplicationWindow_FallsBackToOrganization()
    {
        // Arrange
        var rule = Rule(null, null);
        var organization = Organization(30, 720);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, organization, AlertConfig);

        // Assert
        ((int)alertPolicy.DeduplicationWindow).Should().Be(30);
    }

    [Fact]
    public void Resolve_RuleDeduplicationWindow_WinsOverOrganization()
    {
        // Arrange
        var rule = Rule(15, null);
        var organization = Organization(30, 720);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, organization, AlertConfig);

        // Assert
        ((int)alertPolicy.DeduplicationWindow).Should().Be(15);
    }

    [Fact]
    public void Resolve_RuleSubjectRole_WinsOverConfiguredDefault()
    {
        // Arrange
        var rule = Rule(null, PartyRole.Beneficiary);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, null, AlertConfig);

        // Assert
        alertPolicy.SubjectRole.Should().Be(PartyRole.Beneficiary);
    }

    [Fact]
    public void Resolve_CaseGroupingPeriod_ComesFromOrganizationAndNeverTheRule()
    {
        // Arrange
        var rule = Rule(15, PartyRole.Beneficiary);
        var organization = Organization(30, 720);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, organization, AlertConfig);

        // Assert
        ((int)alertPolicy.CaseGroupingPeriod).Should().Be(720);
    }

    [Fact]
    public void Resolve_NoOrganizationPolicy_LeavesGroupingOffAndSuppressionOn()
    {
        // Arrange
        var rule = Rule(null, null);

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, null, AlertConfig);

        // Assert
        ((bool)alertPolicy.CaseGroupingEnabled).Should().BeFalse();
        ((bool)alertPolicy.DeduplicationEnabled).Should().BeTrue();
        alertPolicy.CaseGroupingScope.Should().Be(CaseGroupingScope.Party);
        alertPolicy.CaseGroupingSeverityPolicy.Should().Be(CaseGroupingSeverityPolicy.Any);
        alertPolicy.MaxAlertsPerCase.Should().BeNull();
    }

    [Fact]
    public void Resolve_OrganizationOptedIn_TakesItsOwnAnswers()
    {
        // Arrange
        var rule = Rule(null, null);
        var organization = FakeOrganization.CreateValid(Fixture) with
        {
            CaseGroupingEnabled = new IsEnabled(true),
            DeduplicationEnabled = new IsEnabled(false),
            CaseGroupingScope = CaseGroupingScope.PartyAndRule,
            CaseGroupingSeverityPolicy = CaseGroupingSeverityPolicy.SameSeverity,
            MaxAlertsPerCase = new MaxAlertsPerCase(25)
        };

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, organization, AlertConfig);

        // Assert
        ((bool)alertPolicy.CaseGroupingEnabled).Should().BeTrue();
        ((bool)alertPolicy.DeduplicationEnabled).Should().BeFalse();
        alertPolicy.CaseGroupingScope.Should().Be(CaseGroupingScope.PartyAndRule);
        alertPolicy.CaseGroupingSeverityPolicy.Should().Be(CaseGroupingSeverityPolicy.SameSeverity);
        ((int)alertPolicy.MaxAlertsPerCase!.Value).Should().Be(25);
    }

    [Fact]
    public void Resolve_MaxAlertsPerCase_HasNoHouseDefault()
    {
        // Arrange
        var rule = Rule(null, null);
        var organization = FakeOrganization.CreateValid(Fixture) with
        {
            MaxAlertsPerCase = null
        };

        // Act
        var alertPolicy = AlertPolicyResolver.Resolve(rule, organization, AlertConfig);

        // Assert
        alertPolicy.MaxAlertsPerCase.Should().BeNull();
    }

    private Rule Rule(int? deduplicationWindow, PartyRole? alertSubjectRole)
    {
        return FakeRule.CreateValid(Fixture) with
        {
            DeduplicationWindow = deduplicationWindow.HasValue ? new DeduplicationWindow(deduplicationWindow.Value) : null,
            AlertSubjectRole = alertSubjectRole
        };
    }

    private Organization Organization(int? deduplicationWindow, int? caseGroupingPeriod)
    {
        return FakeOrganization.CreateValid(Fixture) with
        {
            DeduplicationWindow = deduplicationWindow.HasValue ? new DeduplicationWindow(deduplicationWindow.Value) : null,
            CaseGroupingPeriod = caseGroupingPeriod.HasValue ? new CaseGroupingPeriod(caseGroupingPeriod.Value) : null
        };
    }
}
