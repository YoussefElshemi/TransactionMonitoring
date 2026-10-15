using AutoFixture;
using Presentation.Models.Alerts;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

public static class FakeAlertRequestDto
{
    public static AlertRequestDto CreateValid(IFixture fixture)
    {
        return new AlertRequestDto
        {
            AlertId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
