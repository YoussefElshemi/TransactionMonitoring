using AutoFixture;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeApiClientRequestDto
{
    public static ApiClientRequestDto CreateValid(IFixture fixture)
    {
        return new ApiClientRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
