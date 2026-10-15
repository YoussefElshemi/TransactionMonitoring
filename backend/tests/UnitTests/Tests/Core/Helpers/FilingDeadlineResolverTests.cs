using Core.Configs;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;

namespace UnitTests.Tests.Core.Helpers;

public class FilingDeadlineResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset CaseOpenedAt = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_WithNoPolicy_UsesTheHouseDeadline()
    {
        // Arrange
        var suspiciousActivityReportConfig = new SuspiciousActivityReportConfig();

        // Act
        var dueAt = FilingDeadlineResolver.Resolve(null, suspiciousActivityReportConfig, new CreatedAt(CaseOpenedAt));

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(CaseOpenedAt.AddDays(30));
    }

    [Fact]
    public void Resolve_WithADeclaredDeadline_PrefersItOverTheHouseDefault()
    {
        // Arrange
        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture) with
        {
            FilingDeadline = new FilingDeadline(15)
        };

        // Act
        var dueAt = FilingDeadlineResolver.Resolve(
            suspiciousActivityReportPolicy, new SuspiciousActivityReportConfig(), new CreatedAt(CaseOpenedAt));

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(CaseOpenedAt.AddDays(15));
    }

    [Fact]
    public void Resolve_WithAPolicyDeclaringNoDeadline_FallsBackToTheHouseDefault()
    {
        // Arrange
        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture);

        // Act
        var dueAt = FilingDeadlineResolver.Resolve(
            suspiciousActivityReportPolicy, new SuspiciousActivityReportConfig(), new CreatedAt(CaseOpenedAt));

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(CaseOpenedAt.AddDays(30));
    }

    [Fact]
    public void Resolve_IsMeasuredFromTheCaseOpeningRatherThanFromNow()
    {
        // Arrange
        var caseOpenedAt = new CreatedAt(CaseOpenedAt.AddDays(-21));

        // Act
        var dueAt = FilingDeadlineResolver.Resolve(null, new SuspiciousActivityReportConfig(), caseOpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(CaseOpenedAt.AddDays(9));
    }

    [Fact]
    public void RequiresFilingApproval_WithNoPolicy_IsFalse()
    {
        // Arrange
        // Act
        var requiresApproval = FilingDeadlineResolver.RequiresFilingApproval(null);

        // Assert
        requiresApproval.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequiresFilingApproval_FollowsWhatThePolicyDeclared(bool declared)
    {
        // Arrange
        var suspiciousActivityReportPolicy = FakeSuspiciousActivityReportPolicy.CreateValid(Fixture) with
        {
            RequireFilingApproval = new IsRequired(declared)
        };

        // Act
        var requiresApproval = FilingDeadlineResolver.RequiresFilingApproval(suspiciousActivityReportPolicy);

        // Assert
        requiresApproval.Should().Be(declared);
    }
}
