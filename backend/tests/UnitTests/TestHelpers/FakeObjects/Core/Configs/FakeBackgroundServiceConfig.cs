using AutoFixture;
using Core.Configs;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeBackgroundServiceConfig
{
    public static BackgroundServiceConfig CreateValid(IFixture fixture)
    {
        return new BackgroundServiceConfig
        {
            OutboxEnabled = true,
            OutboxIntervalInMs = fixture.Create<int>(),
            OutboxMaxAttempts = fixture.Create<int>(),
            BackTestChunkSize = 100
        };
    }
}