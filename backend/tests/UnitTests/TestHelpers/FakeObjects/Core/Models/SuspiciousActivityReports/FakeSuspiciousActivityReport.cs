using AutoFixture;
using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.SuspiciousActivityReports;

public static class FakeSuspiciousActivityReport
{
    public static SuspiciousActivityReport CreateValid(IFixture fixture)
    {
        return new SuspiciousActivityReport
        {
            Mode = Mode.Live,
            SuspiciousActivityReportId = FakeSuspiciousActivityReportId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            Status = SuspiciousActivityReportStatus.Draft,
            Narrative = FakeNarrative.CreateValid(fixture),
            DueAt = FakeDueAt.CreateValid(fixture),
            OverdueNotifiedAt = null,
            OpenedByUserId = FakeApplicationUserId.CreateValid(fixture),
            OpenedByUser = null,
            FiledByUserId = null,
            FiledByUser = null,
            FiledAt = null,
            FilingReference = null,
            AcknowledgedAt = null,
            AcknowledgementReference = null,
            NotFiledReason = null,
            DecidedByUserId = null,
            DecidedByUser = null,
            DecidedAt = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
