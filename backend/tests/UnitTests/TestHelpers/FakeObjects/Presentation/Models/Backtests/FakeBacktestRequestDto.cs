using AutoFixture;
using Presentation.Models.BackTests;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

public static class FakeBackTestRequestDto
{
    public static BackTestRequestDto CreateValid(IFixture fixture)
    {
        return new BackTestRequestDto
        {
            BackTestId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
