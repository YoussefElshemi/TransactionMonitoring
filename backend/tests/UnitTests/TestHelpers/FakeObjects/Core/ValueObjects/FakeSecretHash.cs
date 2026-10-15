using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSecretHash
{
    public static SecretHash CreateValid(IFixture fixture)
    {
        return new SecretHash(fixture.Create<string>());
    }
}
