using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeApplicationUserEntity
{
    public static ApplicationUserEntity CreateValid(IFixture fixture)
    {
        return new ApplicationUserEntity
        {
            Id = fixture.Create<Guid>(),
            UserName = fixture.Create<string>(),
            NormalizedUserName = fixture.Create<string>(),
            Email = $"{fixture.Create<string>()}@mail.com",
            NormalizedEmail = $"{fixture.Create<string>()}@mail.com".ToUpper(),
            EmailConfirmed = fixture.Create<bool>(),
            PasswordHash = fixture.Create<string>(),
            SecurityStamp = fixture.Create<string>(),
            ConcurrencyStamp = fixture.Create<string>(),
            PhoneNumber = fixture.Create<string>(),
            PhoneNumberConfirmed = fixture.Create<bool>(),
            TwoFactorEnabled = fixture.Create<bool>(),
            LockoutEnd = fixture.Create<DateTimeOffset>(),
            LockoutEnabled = fixture.Create<bool>(),
            AccessFailedCount = fixture.Create<int>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            ApiClients = [],
            Invoices = [],
            Rules = [],
            RuleSets = [],
            Cases = [],
            Roles = [],
            CaseNotes = []
        };
    }
}