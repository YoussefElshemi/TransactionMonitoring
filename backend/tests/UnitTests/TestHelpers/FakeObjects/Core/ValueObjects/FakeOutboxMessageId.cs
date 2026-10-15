using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeOutboxMessageId
{
    public static OutboxMessageId CreateValid(IFixture fixture)
    {
        return new OutboxMessageId(fixture.Create<Guid>());
    }
}