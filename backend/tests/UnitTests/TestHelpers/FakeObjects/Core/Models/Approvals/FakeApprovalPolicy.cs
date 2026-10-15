using AutoFixture;
using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;

public static class FakeApprovalPolicy
{
    public static ApprovalPolicy CreateValid(IFixture fixture)
    {
        return new ApprovalPolicy
        {
            Mode = Mode.Live,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RequireDeploymentApproval = new IsRequired(false),
            CaseDecisionMinimumSeverity = null,
            CaseDecisionMatcher = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
