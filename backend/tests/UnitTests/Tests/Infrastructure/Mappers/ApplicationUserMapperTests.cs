using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class ApplicationUserMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ApplicationUser()
    {
        // Arrange
        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(Fixture);

        // Act
        var applicationUser = applicationUserEntity.ToModel();

        // Assert
        applicationUser.UserId.Should().Be(new ApplicationUserId(applicationUserEntity.Id));
        applicationUser.Roles.Should().BeEquivalentTo(applicationUserEntity.Roles.Select(x => RoleHelper.MapToUserRole(x.RoleId)).ToList());
        applicationUser.OrganizationId.Should().Be(new OrganizationId(applicationUserEntity.OrganizationId));
        applicationUser.Organization.Should().Be(applicationUserEntity.Organization.ToModel());
        applicationUser.UserName.Should().Be(new UserName(applicationUserEntity.UserName!));
        applicationUser.NormalizedUserName.Should().Be(new NormalizedUserName(applicationUserEntity.NormalizedUserName!));
        applicationUser.Email.Should().Be(new Email(applicationUserEntity.Email!));
        applicationUser.NormalizedEmail.Should().Be(new NormalizedEmail(applicationUserEntity.NormalizedEmail!));
        applicationUser.EmailConfirmed.Should().Be(new EmailConfirmed(applicationUserEntity.EmailConfirmed));
        applicationUser.PasswordHash.Should().Be(new PasswordHash(applicationUserEntity.PasswordHash!));
        applicationUser.SecurityStamp.Should().Be(new SecurityStamp(applicationUserEntity.SecurityStamp!));
        applicationUser.ConcurrencyStamp.Should().Be(new ConcurrencyStamp(applicationUserEntity.ConcurrencyStamp!));
        applicationUser.PhoneNumber.Should().Be(new PhoneNumber(applicationUserEntity.PhoneNumber!));
        applicationUser.PhoneNumberConfirmed.Should().Be(new PhoneNumberConfirmed(applicationUserEntity.PhoneNumberConfirmed));
        applicationUser.TwoFactorEnabled.Should().Be(new TwoFactorEnabled(applicationUserEntity.TwoFactorEnabled));
        applicationUser.LockoutEnd.Should().Be(new LockoutEnd(applicationUserEntity.LockoutEnd!.Value));
        applicationUser.LockoutEnabled.Should().Be(new LockoutEnabled(applicationUserEntity.LockoutEnabled));
        applicationUser.AccessFailedCount.Should().Be(new AccessFailedCount(applicationUserEntity.AccessFailedCount));
        applicationUser.CreatedAt.Should().Be(new CreatedAt(applicationUserEntity.CreatedAt));
        applicationUser.UpdatedAt.Should().Be(new UpdatedAt(applicationUserEntity.UpdatedAt));
        applicationUser.IsDeleted.Should().Be(new IsDeleted(applicationUserEntity.IsDeleted));
        applicationUser.DeletedAt.Should().Be(new DeletedAt(applicationUserEntity.DeletedAt!.Value));
    }
}