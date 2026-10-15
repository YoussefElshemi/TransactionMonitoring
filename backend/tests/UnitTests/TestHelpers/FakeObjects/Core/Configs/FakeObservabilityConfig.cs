using AutoFixture;
using Core.Configs;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeObservabilityConfig
{
    public static ObservabilityConfig CreateValid(IFixture fixture)
    {
        return new ObservabilityConfig
        {
            OtelCollectorEndpoint = fixture.Create<Uri>().ToString(),
            OutputLogsToOtel = false,
            OutputLogsToConsole = false
        };
    }
}