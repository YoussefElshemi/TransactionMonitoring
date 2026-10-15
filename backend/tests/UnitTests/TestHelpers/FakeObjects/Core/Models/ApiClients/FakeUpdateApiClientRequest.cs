using AutoFixture;
using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.ApiClients;

public static class FakeUpdateApiClientRequest
{
    public static UpdateApiClientRequest CreateValid(IFixture fixture)
    {
        return new UpdateApiClientRequest
        {
            ApiClientId = FakeApiClientId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            IsEnabled = new IsEnabled(true),
            Scopes = [ApiScope.RulesEvaluate],
            ApiVersion = null
        };
    }
}
