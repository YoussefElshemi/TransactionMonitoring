using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class CaseMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Case()
    {
        // Arrange
        var caseEntity = FakeCaseEntity.CreateValid(Fixture) with
        {
            TransactionId = Fixture.Create<Guid>()
        };

        // Act
        var @case = caseEntity.ToModel();

        // Assert
        @case.CaseId.Should().Be(new CaseId(caseEntity.CaseId));
        @case.FriendlyName.Should().Be(new FriendlyName(caseEntity.FriendlyName));
        @case.OrganizationId.Should().Be(new OrganizationId(caseEntity.OrganizationId));
        @case.TransactionId.Should().Be(new TransactionId(caseEntity.TransactionId!.Value));
        @case.Status.Should().Be((CaseStatus)caseEntity.StatusId);
        @case.Severity.Should().Be((RuleSeverity)caseEntity.SeverityId);
        @case.AssignedToUserId.Should().Be(new ApplicationUserId(caseEntity.AssignedToUserId!.Value));
        @case.AssignedToUser.Should().BeEquivalentTo(caseEntity.AssignedToUser!.ToModel());
        @case.AssignedAt.Should().Be(new AssignedAt(caseEntity.AssignedAt!.Value));
        @case.ResolvedAt.Should().Be(new ResolvedAt(caseEntity.ResolvedAt!.Value));
        @case.CreatedAt.Should().Be(new CreatedAt(caseEntity.CreatedAt));
        @case.UpdatedAt.Should().Be(new UpdatedAt(caseEntity.UpdatedAt));
        @case.IsDeleted.Should().Be(new IsDeleted(caseEntity.IsDeleted));
        @case.DeletedAt.Should().Be(new DeletedAt(caseEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_Case()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);

        // Act
        var caseEntity = @case.ToEntity();

        // Assert
        caseEntity.CaseId.Should().Be(@case.CaseId);
        caseEntity.FriendlyName.Should().Be(@case.FriendlyName);
        caseEntity.OrganizationId.Should().Be(@case.OrganizationId);
        caseEntity.TransactionId.Should().Be(@case.TransactionId!.Value);
        caseEntity.StatusId.Should().Be((int)@case.Status);
        caseEntity.SeverityId.Should().Be((int)@case.Severity);
        caseEntity.AssignedToUserId.Should().Be(@case.AssignedToUserId!.Value);
        caseEntity.AssignedAt.Should().Be(@case.AssignedAt!.Value);
        caseEntity.ResolvedAt.Should().Be(@case.ResolvedAt!.Value);
        caseEntity.CreatedAt.Should().Be(@case.CreatedAt);
        caseEntity.UpdatedAt.Should().Be(@case.UpdatedAt);
        caseEntity.IsDeleted.Should().Be(@case.IsDeleted);
        caseEntity.DeletedAt.Should().Be(@case.DeletedAt!.Value);
    }

    [Fact]
    public void ToModel_Case_WithoutATransaction()
    {
        var caseEntity = FakeCaseEntity.CreateValid(Fixture) with
        {
            TransactionId = null
        };

        var @case = caseEntity.ToModel();

        @case.TransactionId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_Case_WithoutATransaction()
    {
        var @case = FakeCase.CreateValid(Fixture) with
        {
            TransactionId = null
        };

        var caseEntity = @case.ToEntity();

        caseEntity.TransactionId.Should().BeNull();
    }
}