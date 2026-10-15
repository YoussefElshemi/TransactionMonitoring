using AutoFixture;
using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;

public static class FakeSuspiciousActivityReportPolicy
{
    public static SuspiciousActivityReportPolicy CreateValid(IFixture fixture)
    {
        return new SuspiciousActivityReportPolicy
        {
            Mode = Mode.Live,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            ReportingEntityId = FakeReportingEntityId.CreateValid(fixture),
            FilingDeadline = null,
            RequireFilingApproval = new IsRequired(false),
            AmountPath = null,
            CurrencyPath = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
