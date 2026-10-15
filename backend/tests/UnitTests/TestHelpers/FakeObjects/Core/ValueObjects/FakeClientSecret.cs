using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeClientSecret
{
    public static ClientSecret CreateValid(IFixture fixture)
    {
        return new ClientSecret(fixture.Create<string>());
    }
}
