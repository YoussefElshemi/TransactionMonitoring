using Core.Configs;
using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.Tests.Core.Mappers;

public class CreateCaseRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateCaseRequest()
    {
        // Arrange
        var createCaseRequest = FakeCreateCaseRequest.CreateValid(Fixture);

        // Act
        var @case = createCaseRequest.ToModel(new Dictionary<RuleSeverity, SlaTarget>(), new CaseConfig(), MockTimeProvider, Mode.Live);

        // Assert
        @case.CaseId.Should().NotBeNull();
        @case.Status.Should().Be(CaseStatus.Pending);
        @case.Severity.Should().Be(createCaseRequest.Severity);
        @case.OrganizationId.Should().Be(createCaseRequest.OrganizationId);
        @case.TransactionId.Should().Be(createCaseRequest.TransactionId);
        @case.AssignedToUserId.Should().BeNull();
        @case.AssignedToUser.Should().BeNull();
        @case.AssignedAt.Should().BeNull();
        @case.EscalatedAt.Should().BeNull();
        @case.EscalationRuleId.Should().BeNull();
        @case.ResolvedAt.Should().BeNull();
        @case.IsDeleted.Should().Be(new IsDeleted(false));
        @case.DeletedAt.Should().BeNull();
    }
}