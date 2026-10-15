using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeListRuleRequestDto
{
    public static ListRuleRequestDto CreateValid(IFixture fixture)
    {
        return new ListRuleRequestDto
        {
            RuleSetId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}