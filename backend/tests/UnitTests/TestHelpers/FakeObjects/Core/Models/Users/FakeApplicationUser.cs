using AutoFixture;
using Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

public static class FakeApplicationUser
{
    public static ApplicationUser CreateValid(IFixture fixture)
    {
        return new ApplicationUser
        {
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            UserId = FakeApplicationUserId.CreateValid(fixture),
            SubOrganizationId = null,
            Locale = null,
            TimeZone = null,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Organization = FakeOrganization.CreateValid(fixture),
            UserName = FakeUserName.CreateValid(fixture),
            NormalizedUserName = FakeNormalizedUserName.CreateValid(fixture),
            Email = FakeEmail.CreateValid(fixture),
            NormalizedEmail = FakeNormalizedEmail.CreateValid(fixture),
            EmailConfirmed = FakeEmailConfirmed.CreateValid(fixture),
            PasswordHash = FakePasswordHash.CreateValid(fixture),
            SecurityStamp = FakeSecurityStamp.CreateValid(fixture),
            ConcurrencyStamp = FakeConcurrencyStamp.CreateValid(fixture),
            PhoneNumber = FakePhoneNumber.CreateValid(fixture),
            PhoneNumberConfirmed = FakePhoneNumberConfirmed.CreateValid(fixture),
            TwoFactorEnabled = FakeTwoFactorEnabled.CreateValid(fixture),
            LockoutEnd = FakeLockoutEnd.CreateValid(fixture),
            LockoutEnabled = FakeLockoutEnabled.CreateValid(fixture),
            AccessFailedCount = FakeAccessFailedCount.CreateValid(fixture),
            Roles = []
        };
    }
}