using AutoFixture;
using Presentation.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleEvaluations;

public static class FakeListRuleEvaluationRequestDto
{
    public static ListRuleEvaluationRequestDto CreateValid(IFixture fixture)
    {
        return new ListRuleEvaluationRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
