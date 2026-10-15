using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeListRuleVersionRequestDto
{
    public static ListRuleVersionRequestDto CreateValid(IFixture fixture)
    {
        return new ListRuleVersionRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}
