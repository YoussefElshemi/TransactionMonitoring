using AutoFixture;
using Presentation.Models.Usages;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Usages;

public static class FakeUsageRequestDto
{
    public static UsageRequestDto CreateValid(IFixture fixture)
    {
        return new UsageRequestDto
        {
            UsageId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}