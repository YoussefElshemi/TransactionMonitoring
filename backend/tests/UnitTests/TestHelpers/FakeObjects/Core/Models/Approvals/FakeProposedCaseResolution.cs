using AutoFixture;
using Core.Enums;
using Core.Models.Approvals;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;

public static class FakeProposedCaseResolution
{
    public static ProposedCaseResolution CreateValid(IFixture fixture)
    {
        return new ProposedCaseResolution
        {
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null
        };
    }
}
