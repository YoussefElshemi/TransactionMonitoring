using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeMessage
{
    public static Message CreateValid(IFixture fixture)
    {
        return new Message(fixture.Create<string>());
    }
}