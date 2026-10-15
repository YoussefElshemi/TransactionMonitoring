using AutoFixture;
using Core.Models.ApiClients;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeAuthenticateApiClientRequest
{
    public static AuthenticateApiClientRequest CreateValid(IFixture fixture)
    {
        return new AuthenticateApiClientRequest
        {
            ClientId = FakeClientId.CreateValid(fixture),
            ClientSecret = FakeClientSecret.CreateValid(fixture),
            Scopes = null
        };
    }
}
