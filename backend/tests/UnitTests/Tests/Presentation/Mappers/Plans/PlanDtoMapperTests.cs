using FluentAssertions;
using Presentation.Mappers.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

namespace UnitTests.Tests.Presentation.Mappers.Plans;

public class PlanDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Plan()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture);

        // Act
        var dto = plan.ToDto();

        // Assert
        dto.PlanId.Should().Be(plan.PlanId);
        dto.Name.Should().Be(plan.Name);
        dto.Description.Should().Be(plan.Description.ToString());
        dto.OrganizationId.Should().Be(plan.OrganizationId!.Value);
        dto.IsEnabled.Should().Be(plan.IsEnabled);
        dto.CurrentVersionNumber.Should().Be(plan.CurrentVersionNumber);
        dto.CurrentVersion.Should().BeEquivalentTo(plan.CurrentVersion!.ToDto());
        dto.CreatedAt.Should().Be(plan.CreatedAt);
        dto.UpdatedAt.Should().Be(plan.UpdatedAt);
    }

    [Fact]
    public void ToDto_CatalogPlan_KeepsTheOwnerNull()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture) with { OrganizationId = null, Description = null, CurrentVersion = null };

        // Act
        var dto = plan.ToDto();

        // Assert
        dto.OrganizationId.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.CurrentVersion.Should().BeNull();
    }

    [Fact]
    public void ToDto_PlanVersion()
    {
        // Arrange
        var planVersion = FakePlanVersion.CreateValid(Fixture);

        // Act
        var dto = planVersion.ToDto();

        // Assert
        dto.PlanVersionId.Should().Be(planVersion.PlanVersionId);
        dto.PlanId.Should().Be(planVersion.PlanId);
        dto.VersionNumber.Should().Be(planVersion.VersionNumber);
        dto.CostPerRequest.Should().Be(planVersion.CostPerRequest);
        dto.PublishedByUserId.Should().Be(planVersion.PublishedByUserId!.Value);
        dto.CreatedAt.Should().Be(planVersion.CreatedAt);
    }
}
