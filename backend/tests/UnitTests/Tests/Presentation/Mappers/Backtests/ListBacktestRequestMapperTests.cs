using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Mappers.BackTests;

public class ListBackTestRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListBackTestRequestDto()
    {
        // Arrange
        var listBackTestRequestDto = FakeListBackTestRequestDto.CreateValid(Fixture);

        // Act
        var listBackTestRequest = listBackTestRequestDto.ToModel();

        // Assert
        listBackTestRequest.OrganizationId.Should().Be(new OrganizationId(listBackTestRequestDto.OrganizationId));
        listBackTestRequest.RuleId.Should().Be(new RuleId(listBackTestRequestDto.RuleId!.Value));
        listBackTestRequest.PageNumber.Should().Be(new PageNumber(listBackTestRequestDto.PageNumber));
        listBackTestRequest.PageSize.Should().Be(new PageSize(listBackTestRequestDto.PageSize));
        listBackTestRequest.FromDate.Should().Be(new DateRange(listBackTestRequestDto.FromDate));
        listBackTestRequest.ToDate.Should().Be(new DateRange(listBackTestRequestDto.ToDate));
    }
}
