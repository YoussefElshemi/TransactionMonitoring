using AutoFixture;
using Presentation.Models.RuleSets;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeListRuleSetRequestDto
{
    public static ListRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new ListRuleSetRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}