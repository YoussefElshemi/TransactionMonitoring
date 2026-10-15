using AutoFixture;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Mappers.Plans;

public class ListPlanRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenListPlanRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeListPlanRequestDto.CreateValid(Fixture);

        // Act
        var model = dto.ToModel();

        // Assert
        model.Name.Should().Be(new PlanName(dto.Name!));
        model.IncludeDisabled.Should().Be(new IncludeDisabled(dto.IncludeDisabled!.Value));
        model.PageNumber.Should().Be(new PageNumber(dto.PageNumber));
        model.PageSize.Should().Be(new PageSize(dto.PageSize));
        model.FromDate.Should().Be(new DateRange(dto.FromDate));
        model.ToDate.Should().Be(new DateRange(dto.ToDate));
    }

    [Fact]
    public void ToModel_GivenAnOwnerOrganizationId_MapsItRatherThanTheBoundOrganizationId()
    {
        // Arrange
        var dto = FakeListPlanRequestDto.CreateValid(Fixture);
        dto.OrganizationId = Fixture.Create<Guid>();

        // Act
        var model = dto.ToModel();

        // Assert
        model.OrganizationId.Should().Be(new OrganizationId(dto.OwnerOrganizationId!.Value));
    }

    [Fact]
    public void ToModel_GivenNoOwnerOrganizationId_MapsToNull()
    {
        // Arrange
        var dto = FakeListPlanRequestDto.CreateValid(Fixture) with { OwnerOrganizationId = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.OrganizationId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToModel_GivenNoName_MapsToNull(string? name)
    {
        // Arrange
        var dto = FakeListPlanRequestDto.CreateValid(Fixture) with { Name = name };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Name.Should().BeNull();
    }

    [Fact]
    public void ToModel_GivenNoIncludeDisabled_DefaultsToFalse()
    {
        // Arrange
        var dto = FakeListPlanRequestDto.CreateValid(Fixture) with { IncludeDisabled = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.IncludeDisabled.Should().Be(new IncludeDisabled(false));
    }

    [Fact]
    public void ToModel_GivenListPlanVersionRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeListPlanVersionRequestDto.CreateValid(Fixture);

        // Act
        var model = dto.ToModel();

        // Assert
        model.PlanId.Should().Be(new PlanId(dto.PlanId));
        model.PageNumber.Should().Be(new PageNumber(dto.PageNumber));
        model.PageSize.Should().Be(new PageSize(dto.PageSize));
        model.FromDate.Should().Be(new DateRange(dto.FromDate));
        model.ToDate.Should().Be(new DateRange(dto.ToDate));
    }
}
