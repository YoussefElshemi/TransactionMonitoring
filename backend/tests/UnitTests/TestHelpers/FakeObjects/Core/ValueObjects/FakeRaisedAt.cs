using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRaisedAt
{
    public static RaisedAt CreateValid(IFixture fixture)
    {
        return new RaisedAt(fixture.Create<DateTimeOffset>());
    }
}
