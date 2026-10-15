using AutoFixture;
using Core.Enums;
using Presentation.Models.Decisions;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Decisions;

public static class FakeResolveDecisionRequestDto
{
    public static ResolveDecisionRequestDto CreateValid(IFixture fixture)
    {
        return new ResolveDecisionRequestDto
        {
            DecisionId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new ResolveDecisionRequestBodyDto
            {
                Status = nameof(CaseStatus.Approved),
                UserId = fixture.Create<Guid>()
            }
        };
    }
}
