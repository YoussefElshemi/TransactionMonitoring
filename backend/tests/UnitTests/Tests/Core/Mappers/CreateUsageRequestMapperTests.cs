using Core.Enums;
using Core.Mappers;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;

namespace UnitTests.Tests.Core.Mappers;

public class CreateUsageRequestMapperMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateUsageRequestMapper()
    {
        // Arrange
        var createUsageRequest = FakeCreateUsageRequest.CreateValid(Fixture);

        // Act
        var rule = createUsageRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        rule.Resource.Should().Be(createUsageRequest.Resource);
        rule.OrganizationId.Should().Be(createUsageRequest.OrganizationId);
        rule.Count.Should().Be(createUsageRequest.Count);
    }
}