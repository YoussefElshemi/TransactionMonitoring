using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSecretPrefix
{
    public static SecretPrefix CreateValid(IFixture fixture)
    {
        return new SecretPrefix(fixture.Create<string>());
    }
}
