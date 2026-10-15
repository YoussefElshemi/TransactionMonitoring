using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Core.ValueObjects;
using FluentValidation;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

namespace UnitTests.Tests.Core.Extensions;

public class ApplicationUserExtensionsTests : BaseTestClass
{
    [Fact]
    public void ThrowIfNotEnabled_UserIsEnabled_DoesNotThrow()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture);

        // Act && Assert
        applicationUser.ThrowIfNotEnabled();
    }

    [Fact]
    public void ThrowIfNotEnabled_UserIsNotEnabled_DoesThrow()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization)
        };

        // Act && Assert
        Assert.Throws<ValidationException>(() => applicationUser.ThrowIfNotEnabled());
    }

    [Fact]
    public void ThrowIfOrganizationNotEnabled_OrganizationIsEnabled_DoesNotThrow()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture) with
        {
            Organization = FakeOrganization.CreateValid(Fixture) with
            {
                Status = OrganizationStatus.Enabled
            }
        };

        // Act && Assert
        applicationUser.ThrowIfOrganizationNotEnabled();
    }

    [Fact]
    public void ThrowIfOrganizationNotEnabled_OrganizationIsDisabled_DoesThrow()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture) with
        {
            Organization = FakeOrganization.CreateValid(Fixture) with
            {
                Status = OrganizationStatus.Disabled
            }
        };

        // Act && Assert
        Assert.Throws<ValidationException>(() => applicationUser.ThrowIfOrganizationNotEnabled());
    }

    [Fact]
    public void ThrowIfOrganizationNotEnabled_OrganizationOverdueInvoice_DoesThrow()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture) with
        {
            Organization = FakeOrganization.CreateValid(Fixture) with
            {
                Status = OrganizationStatus.OverdueInvoice
            }
        };

        // Act && Assert
        Assert.Throws<ValidationException>(() => applicationUser.ThrowIfOrganizationNotEnabled());
    }
}