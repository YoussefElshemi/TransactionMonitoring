using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Mappers.Subscriptions;

public class ListSubscriptionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_GivenListSubscriptionRequestDto_MapsToModel()
    {
        // Arrange
        var dto = FakeListSubscriptionRequestDto.CreateValid(Fixture) with { IncludeSuperseded = true };

        // Act
        var model = dto.ToModel();

        // Assert
        model.OrganizationId.Should().Be(new OrganizationId(dto.OrganizationId));
        model.IncludeSuperseded.Should().Be(new IncludeSuperseded(true));
        model.PageNumber.Should().Be(new PageNumber(dto.PageNumber));
        model.PageSize.Should().Be(new PageSize(dto.PageSize));
        model.FromDate.Should().Be(new DateRange(dto.FromDate));
        model.ToDate.Should().Be(new DateRange(dto.ToDate));
    }

    [Fact]
    public void ToModel_GivenNoIncludeSuperseded_DefaultsToFalse()
    {
        // Arrange
        var dto = FakeListSubscriptionRequestDto.CreateValid(Fixture) with { IncludeSuperseded = null };

        // Act
        var model = dto.ToModel();

        // Assert
        model.IncludeSuperseded.Should().Be(new IncludeSuperseded(false));
    }
}
