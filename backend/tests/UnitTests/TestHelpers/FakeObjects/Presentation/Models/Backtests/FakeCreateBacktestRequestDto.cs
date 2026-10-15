using AutoFixture;
using Presentation.Models.BackTests;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

public static class FakeCreateBackTestRequestDto
{
    public static CreateBackTestRequestDto CreateValid(IFixture fixture)
    {
        return new CreateBackTestRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            Body = FakeCreateBackTestRequestBodyDto.CreateValid(fixture),
            OrganizationId = fixture.Create<Guid>(),
            CreatedByUserId = fixture.Create<Guid>()
        };
    }
}
