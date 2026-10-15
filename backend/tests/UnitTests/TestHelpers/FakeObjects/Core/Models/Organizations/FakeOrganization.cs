using AutoFixture;
using Core.Enums;
using Core.Models.Organizations;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;

public static class FakeOrganization
{
    public static Organization CreateValid(IFixture fixture)
    {
        return new Organization
        {
            SandboxMonthlyCredits = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            Status = fixture.Create<OrganizationStatus>(),
            HoldWindow = null,
            HoldExpiryVerdict = null,
            DeduplicationWindow = null,
            CaseGroupingEnabled = new IsEnabled(true),
            DeduplicationEnabled = new IsEnabled(true),
            CaseGroupingScope = CaseGroupingScope.Party,
            CaseGroupingSeverityPolicy = CaseGroupingSeverityPolicy.Any,
            MaxAlertsPerCase = null,
            CaseGroupingPeriod = null,
            DefaultLocale = null,
            DefaultTimeZone = null
        };
    }
}