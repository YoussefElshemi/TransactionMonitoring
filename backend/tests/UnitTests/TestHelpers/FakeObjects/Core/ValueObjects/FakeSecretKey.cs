using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSecretKey
{
    public static SecretKey CreateValid(IFixture fixture)
    {
        return new SecretKey(fixture.Create<string>());
    }
}