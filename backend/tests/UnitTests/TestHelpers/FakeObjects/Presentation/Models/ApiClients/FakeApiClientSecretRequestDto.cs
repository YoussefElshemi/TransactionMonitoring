using AutoFixture;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeApiClientSecretRequestDto
{
    public static ApiClientSecretRequestDto CreateValid(IFixture fixture)
    {
        return new ApiClientSecretRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            ApiClientSecretId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
