using AutoFixture;
using Presentation.Models.ApiClients;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.ApiClients;

public static class FakeCreateApiClientSecretRequestDto
{
    public static CreateApiClientSecretRequestDto CreateValid(IFixture fixture)
    {
        return new CreateApiClientSecretRequestDto
        {
            ApiClientId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateApiClientSecretRequestBodyDto
            {
                ExpiresAt = null
            }
        };
    }
}
