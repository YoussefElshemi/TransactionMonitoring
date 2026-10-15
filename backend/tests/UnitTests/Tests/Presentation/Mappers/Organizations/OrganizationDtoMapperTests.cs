using FluentAssertions;
using Presentation.Mappers.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;

namespace UnitTests.Tests.Presentation.Mappers.Organizations;

public class OrganizationDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Organization()
    {
        // Arrange
        var organization = FakeOrganization.CreateValid(Fixture);

        // Act
        var organizationDto = organization.ToDto();

        organizationDto.OrganizationId.Should().Be(organization.OrganizationId);
        organizationDto.FriendlyName.Should().Be(organization.FriendlyName);
        organizationDto.Status.Should().Be(organization.Status.ToString());
        organizationDto.CreatedAt.Should().Be(organization.CreatedAt);
        organizationDto.UpdatedAt.Should().Be(organization.UpdatedAt);
    }
}