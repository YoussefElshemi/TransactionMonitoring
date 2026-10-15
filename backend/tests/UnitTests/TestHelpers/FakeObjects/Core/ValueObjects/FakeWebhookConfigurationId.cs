using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeWebhookConfigurationId
{
    public static WebhookConfigurationId CreateValid(IFixture fixture)
    {
        return new WebhookConfigurationId(fixture.Create<Guid>());
    }
}