using AutoFixture;
using FluentAssertions;
using Presentation.Mappers.Common;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Common;

namespace UnitTests.Tests.Presentation.Mappers.Common;

public class PagedResponseMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Should_Map_PagedResponse_To_PagedResponseDto_Correctly()
    {
        // Arrange
        var pagedResponse = FakePagedResponse.CreateValid(Fixture, () => Fixture.Create<string>());

        // Act
        var pagedResponseDto = pagedResponse.ToDto(s => s);

        // Assert
        pagedResponseDto.Metadata.PageNumber.Should().Be(pagedResponse.PageNumber);
        pagedResponseDto.Metadata.PageSize.Should().Be(pagedResponse.PageSize);
        pagedResponseDto.Metadata.PageCount.Should().Be(pagedResponse.PageCount);
        pagedResponseDto.Metadata.TotalCount.Should().Be(pagedResponse.TotalCount);
        pagedResponseDto.Metadata.HasMore.Should().Be(pagedResponse.HasMore);
        pagedResponseDto.Data.Should().BeEquivalentTo(pagedResponse.Data);
    }
}