using AutoFixture;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeRecoverApiClientRequestDto
{
    public static RecoverApiClientRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverApiClientRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
