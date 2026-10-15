using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeClientId
{
    public static ClientId CreateValid(IFixture fixture)
    {
        return new ClientId(fixture.Create<string>());
    }
}
