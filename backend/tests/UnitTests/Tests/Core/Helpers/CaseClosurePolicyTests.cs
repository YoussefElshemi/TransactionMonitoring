using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseReasonCodes;

namespace UnitTests.Tests.Core.Helpers;

public class CaseClosurePolicyTests : BaseTestClass
{
    [Theory]
    [InlineData(CaseStatus.Rejected, CaseDisposition.TruePositive)]
    [InlineData(CaseStatus.Rejected, CaseDisposition.FalsePositive)]
    [InlineData(CaseStatus.Rejected, CaseDisposition.TrueNegative)]
    [InlineData(CaseStatus.Rejected, CaseDisposition.FalseNegative)]
    [InlineData(CaseStatus.Approved, CaseDisposition.TruePositive)]
    [InlineData(CaseStatus.Approved, CaseDisposition.FalseNegative)]
    public void RequiresNote_GivenARejectionATruePositiveOrAMiss_IsTrue(CaseStatus status, CaseDisposition disposition)
    {
        // Arrange & Act
        var requiresNote = CaseClosurePolicy.RequiresNote(status, disposition);

        // Assert
        requiresNote.Should().BeTrue();
    }

    [Theory]
    [InlineData(CaseDisposition.FalsePositive)]
    [InlineData(CaseDisposition.TrueNegative)]
    public void RequiresNote_GivenAnApprovedCloseThatFoundNothing_IsFalse(CaseDisposition disposition)
    {
        // Arrange & Act
        var requiresNote = CaseClosurePolicy.RequiresNote(CaseStatus.Approved, disposition);

        // Assert
        requiresNote.Should().BeFalse();
    }

    [Fact]
    public void RequiresNote_GivenNoDispositionOnAnApprovedSettle_IsFalse()
    {
        // Arrange & Act
        var requiresNote = CaseClosurePolicy.RequiresNote(CaseStatus.Approved, null);

        // Assert
        requiresNote.Should().BeFalse();
    }

    [Fact]
    public void PermitsDisposition_GivenACodeWithNoDisposition_IsTrueForEveryOne()
    {
        // Arrange
        var caseReasonCode = FakeCaseReasonCode.CreateValid(Fixture) with
        {
            Disposition = null
        };

        // Act
        var permits = Enum.GetValues<CaseDisposition>().Select(caseReasonCode.PermitsDisposition);

        // Assert
        permits.Should().AllSatisfy(x => x.Should().BeTrue());
    }

    [Theory]
    [InlineData(CaseDisposition.TruePositive, CaseDisposition.FalsePositive)]
    [InlineData(CaseDisposition.FalsePositive, CaseDisposition.TruePositive)]
    [InlineData(CaseDisposition.FalseNegative, CaseDisposition.TruePositive)]
    [InlineData(CaseDisposition.TrueNegative, CaseDisposition.FalseNegative)]
    public void PermitsDisposition_GivenACodeBoundToAnotherDisposition_IsFalse(CaseDisposition bound, CaseDisposition chosen)
    {
        // Arrange
        var caseReasonCode = FakeCaseReasonCode.CreateValid(Fixture) with
        {
            Disposition = bound
        };

        // Act
        var permits = caseReasonCode.PermitsDisposition(chosen);

        // Assert
        permits.Should().BeFalse();
    }

    [Fact]
    public void PermitsDisposition_GivenACodeBoundToTheSameDisposition_IsTrue()
    {
        // Arrange
        var caseReasonCode = FakeCaseReasonCode.CreateValid(Fixture) with
        {
            Disposition = CaseDisposition.TruePositive
        };

        // Act
        var permits = caseReasonCode.PermitsDisposition(CaseDisposition.TruePositive);

        // Assert
        permits.Should().BeTrue();
    }
}
