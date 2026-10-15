using AutoFixture;
using Core.Configs;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeAppConfig
{
    public static AppConfig CreateValid(IFixture fixture)
    {
        return new AppConfig
        {
            ObservabilityConfig = FakeObservabilityConfig.CreateValid(fixture),
            BackgroundServiceConfig = FakeBackgroundServiceConfig.CreateValid(fixture),
            InvoiceConfig = FakeInvoiceConfig.CreateValid(fixture),
            IdempotencyConfig = FakeIdempotencyConfig.CreateValid(fixture)
        };
    }
}