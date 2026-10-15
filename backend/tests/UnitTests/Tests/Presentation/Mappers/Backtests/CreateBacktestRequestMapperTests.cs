using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Mappers.BackTests;

public class CreateBackTestRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateBackTestRequestDto()
    {
        // Arrange
        var createBackTestRequestDto = FakeCreateBackTestRequestDto.CreateValid(Fixture);

        // Act
        var createBackTestRequest = createBackTestRequestDto.ToModel();

        // Assert
        createBackTestRequest.OrganizationId.Should().Be(new OrganizationId(createBackTestRequestDto.OrganizationId));
        createBackTestRequest.RuleId.Should().Be(new RuleId(createBackTestRequestDto.RuleId));
        createBackTestRequest.SampleSize.Should().Be(new SampleSize(createBackTestRequestDto.Body!.SampleSize));
        createBackTestRequest.CreatedByUserId.Should().Be(new ApplicationUserId(createBackTestRequestDto.CreatedByUserId));
    }

    [Fact]
    public void ToModel_MapsVersionNumber_WhenProvided()
    {
        // Arrange
        var createBackTestRequestDto = FakeCreateBackTestRequestDto.CreateValid(Fixture);
        createBackTestRequestDto = createBackTestRequestDto with
        {
            Body = createBackTestRequestDto.Body! with { VersionNumber = 3 }
        };

        // Act
        var createBackTestRequest = createBackTestRequestDto.ToModel();

        // Assert
        createBackTestRequest.VersionNumber.Should().Be(new VersionNumber(3));
    }

    [Fact]
    public void ToModel_VersionNumberIsNull_WhenNotProvided()
    {
        // Arrange
        var createBackTestRequestDto = FakeCreateBackTestRequestDto.CreateValid(Fixture);
        createBackTestRequestDto = createBackTestRequestDto with
        {
            Body = createBackTestRequestDto.Body! with { VersionNumber = null }
        };

        // Act
        var createBackTestRequest = createBackTestRequestDto.ToModel();

        // Assert
        createBackTestRequest.VersionNumber.Should().BeNull();
    }
}
