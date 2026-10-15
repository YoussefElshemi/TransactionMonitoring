using AutoFixture;
using Core.Enums;
using Core.Models.Approvals;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;

public static class FakeApproval
{
    public static Approval CreateValid(IFixture fixture)
    {
        return new Approval
        {
            Mode = Mode.Live,
            ApprovalId = FakeApprovalId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Subject = ApprovalSubject.CaseResolution,
            RuleDeploymentId = null,
            CaseId = FakeCaseId.CreateValid(fixture),
            SuspiciousActivityReportId = null,
            ProposedCaseResolution = FakeProposedCaseResolution.CreateValid(fixture),
            Status = ApprovalStatus.Pending,
            Reason = null,
            RequestedByUserId = FakeApplicationUserId.CreateValid(fixture),
            RequestedByUser = null,
            ReviewedByUserId = null,
            ReviewedByUser = null,
            ReviewedAt = null,
            ReviewNote = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
