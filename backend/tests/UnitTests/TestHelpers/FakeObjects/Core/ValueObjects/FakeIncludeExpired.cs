using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIncludeExpired
{
    public static IncludeExpired CreateValid(IFixture fixture)
    {
        return new IncludeExpired(fixture.Create<bool>());
    }
}
