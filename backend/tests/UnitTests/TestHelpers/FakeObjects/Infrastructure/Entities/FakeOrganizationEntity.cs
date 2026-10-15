using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeOrganizationEntity
{
    public static OrganizationEntity CreateValid(IFixture fixture)
    {
        return new OrganizationEntity
        {
            SandboxMonthlyCredits = null,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            OrganizationId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            StatusId = (int)fixture.Create<OrganizationStatus>(),
            StatusEntity = null!,
            HoldWindow = null,
            HoldExpiryVerdictId = null,
            DeduplicationWindow = null,
            CaseGroupingEnabled = true,
            DeduplicationEnabled = true,
            CaseGroupingScopeId = (int)CaseGroupingScope.Party,
            CaseGroupingSeverityPolicyId = (int)CaseGroupingSeverityPolicy.Any,
            MaxAlertsPerCase = null,
            CaseGroupingPeriod = null,
            DefaultLocale = null,
            DefaultTimeZone = null
        };
    }
}