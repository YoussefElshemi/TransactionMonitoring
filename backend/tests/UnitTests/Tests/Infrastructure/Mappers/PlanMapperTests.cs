using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class PlanMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Plan()
    {
        // Arrange
        var planEntity = FakePlanEntity.CreateValid(Fixture);
        var planVersionEntity = FakePlanVersionEntity.CreateValid(Fixture) with
        {
            PlanId = planEntity.PlanId,
            VersionNumber = planEntity.CurrentVersionNumber
        };

        planEntity.PlanVersions.Add(planVersionEntity);

        // Act
        var plan = planEntity.ToModel();

        // Assert
        plan.PlanId.Should().Be(new PlanId(planEntity.PlanId));
        plan.Name.Should().Be(new PlanName(planEntity.Name));
        plan.Description.Should().Be(new PlanDescription(planEntity.Description!));
        plan.OrganizationId.Should().Be(new OrganizationId(planEntity.OrganizationId!.Value));
        plan.IsEnabled.Should().Be(new IsEnabled(planEntity.IsEnabled));
        plan.CurrentVersionNumber.Should().Be(new VersionNumber(planEntity.CurrentVersionNumber));
        plan.CurrentVersion.Should().BeEquivalentTo(planVersionEntity.ToModel());
        plan.CreatedAt.Should().Be(new CreatedAt(planEntity.CreatedAt));
        plan.UpdatedAt.Should().Be(new UpdatedAt(planEntity.UpdatedAt));
        plan.IsDeleted.Should().Be(new IsDeleted(planEntity.IsDeleted));
        plan.DeletedAt.Should().Be(new DeletedAt(planEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToModel_CatalogPlan_KeepsTheOwnerNull()
    {
        // Arrange
        var planEntity = FakePlanEntity.CreateValid(Fixture) with { OrganizationId = null, Description = null };

        // Act
        var plan = planEntity.ToModel();

        // Assert
        plan.OrganizationId.Should().BeNull();
        plan.Description.Should().BeNull();
        plan.CurrentVersion.Should().BeNull();
    }

    [Fact]
    public void ToEntity_Plan()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture);

        // Act
        var planEntity = plan.ToEntity();

        // Assert
        planEntity.PlanId.Should().Be(plan.PlanId);
        planEntity.Name.Should().Be(plan.Name);
        planEntity.Description.Should().Be(plan.Description.ToString());
        planEntity.OrganizationId.Should().Be(plan.OrganizationId!.Value);
        planEntity.IsEnabled.Should().Be(plan.IsEnabled);
        planEntity.CurrentVersionNumber.Should().Be(plan.CurrentVersionNumber);
        planEntity.CreatedAt.Should().Be(plan.CreatedAt);
        planEntity.UpdatedAt.Should().Be(plan.UpdatedAt);
        planEntity.IsDeleted.Should().Be(plan.IsDeleted);
        planEntity.DeletedAt.Should().Be(plan.DeletedAt!.Value);
    }
}
