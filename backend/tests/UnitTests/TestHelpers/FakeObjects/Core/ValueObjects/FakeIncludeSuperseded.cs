using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIncludeSuperseded
{
    public static IncludeSuperseded CreateValid(IFixture fixture)
    {
        return new IncludeSuperseded(fixture.Create<bool>());
    }
}
