using AutoFixture;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeListRuleDeploymentRequestDto
{
    public static ListRuleDeploymentRequestDto CreateValid(IFixture fixture)
    {
        return new ListRuleDeploymentRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = fixture.Create<int>(),
            PageSize = fixture.Create<int>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}
