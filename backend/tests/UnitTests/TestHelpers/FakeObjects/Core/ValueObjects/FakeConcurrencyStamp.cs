using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeConcurrencyStamp
{
    public static ConcurrencyStamp CreateValid(IFixture fixture)
    {
        return new ConcurrencyStamp(fixture.Create<string>());
    }
}