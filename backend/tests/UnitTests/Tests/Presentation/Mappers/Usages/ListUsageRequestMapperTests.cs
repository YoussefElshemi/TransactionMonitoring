using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Usages;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Usages;

namespace UnitTests.Tests.Presentation.Mappers.Usages;

public class ListUsageRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListUsageRequestDto()
    {
        // Arrange
        var listUsageRequestDto = FakeListUsageRequestDto.CreateValid(Fixture);

        // Act
        var listUsageRequest = listUsageRequestDto.ToModel();

        // Assert
        listUsageRequest.OrganizationId.Should().Be(new OrganizationId(listUsageRequestDto.OrganizationId));
        listUsageRequest.FromDate.Should().Be(new DateRange(listUsageRequestDto.FromDate));
        listUsageRequest.ToDate.Should().Be(new DateRange(listUsageRequestDto.ToDate));
        listUsageRequest.Resource.Should().Be(new Resource(listUsageRequestDto.Resource!));
        listUsageRequest.PageNumber.Should().Be(new PageNumber(listUsageRequestDto.PageNumber));
        listUsageRequest.PageSize.Should().Be(new PageSize(listUsageRequestDto.PageSize));
    }
}