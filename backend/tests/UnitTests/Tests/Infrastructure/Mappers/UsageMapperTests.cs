using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class UsageMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Usage()
    {
        // Arrange
        var apiKeyEntity = FakeUsageEntity.CreateValid(Fixture);

        // Act
        var apiKey = apiKeyEntity.ToModel();

        // Assert
        apiKey.UsageId.Should().Be(new UsageId(apiKeyEntity.UsageId));
        apiKey.Resource.Should().Be(new Resource(apiKeyEntity.Resource));
        apiKey.Count.Should().Be(new Count(apiKeyEntity.Count));
        apiKey.OrganizationId.Should().Be(new OrganizationId(apiKeyEntity.OrganizationId));
        apiKey.CreatedAt.Should().Be(new CreatedAt(apiKeyEntity.CreatedAt));
        apiKey.UpdatedAt.Should().Be(new UpdatedAt(apiKeyEntity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_Usage()
    {
        // Arrange
        var apiKey = FakeUsage.CreateValid(Fixture);

        // Act
        var apiKeyEntity = apiKey.ToEntity();

        // Assert
        apiKeyEntity.UsageId.Should().Be(apiKey.UsageId);
        apiKeyEntity.Resource.Should().Be(apiKey.Resource);
        apiKeyEntity.Count.Should().Be(apiKey.Count);
        apiKeyEntity.OrganizationId.Should().Be(apiKey.OrganizationId);
        apiKeyEntity.CreatedAt.Should().Be(apiKey.CreatedAt);
        apiKeyEntity.UpdatedAt.Should().Be(apiKey.UpdatedAt);
    }
}