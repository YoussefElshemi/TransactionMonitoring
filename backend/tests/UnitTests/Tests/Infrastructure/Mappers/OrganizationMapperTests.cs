using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class OrganizationMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Organization()
    {
        // Arrange
        var organizationEntity = FakeOrganizationEntity.CreateValid(Fixture);

        // Act
        var organization = organizationEntity.ToModel();

        // Assert
        organization.OrganizationId.Should().Be(new OrganizationId(organizationEntity.OrganizationId));
        organization.FriendlyName.Should().Be(new FriendlyName(organizationEntity.FriendlyName));
        organization.Status.Should().Be((OrganizationStatus)organizationEntity.StatusId);
        organization.CreatedAt.Should().Be(new CreatedAt(organizationEntity.CreatedAt));
        organization.UpdatedAt.Should().Be(new UpdatedAt(organizationEntity.UpdatedAt));
        organization.IsDeleted.Should().Be(new IsDeleted(organizationEntity.IsDeleted));
        organization.DeletedAt.Should().Be(new DeletedAt(organizationEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_Organization()
    {
        // Arrange
        var organization = FakeOrganization.CreateValid(Fixture);

        // Act
        var organizationEntity = organization.ToEntity();

        // Assert
        organizationEntity.OrganizationId.Should().Be(organization.OrganizationId);
        organizationEntity.FriendlyName.Should().Be(organization.FriendlyName);
        organizationEntity.StatusId.Should().Be((int)organization.Status);
        organizationEntity.OrganizationId.Should().Be(organization.OrganizationId);
        organizationEntity.CreatedAt.Should().Be(organization.CreatedAt);
        organizationEntity.UpdatedAt.Should().Be(organization.UpdatedAt);
        organizationEntity.IsDeleted.Should().Be(organization.IsDeleted);
        organizationEntity.DeletedAt.Should().Be(organization.DeletedAt!.Value);
    }
}