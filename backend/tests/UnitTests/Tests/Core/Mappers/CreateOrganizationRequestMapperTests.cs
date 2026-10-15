using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;

namespace UnitTests.Tests.Core.Mappers;

public class CreateOrganizationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateOrganizationRequest()
    {
        // Arrange
        var createOrganizationRequest = FakeCreateOrganizationRequest.CreateValid(Fixture);

        // Act
        var organization = createOrganizationRequest.ToModel(MockTimeProvider);

        // Assert
        organization.OrganizationId.Should().NotBeNull();
        organization.FriendlyName.Should().Be(createOrganizationRequest.FriendlyName);
        organization.Status.Should().Be(createOrganizationRequest.Status);
        organization.IsDeleted.Should().Be(new IsDeleted(false));
        organization.DeletedAt.Should().BeNull();
    }
}