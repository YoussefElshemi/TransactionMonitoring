using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIncludeApplied
{
    public static IncludeApplied CreateValid(IFixture fixture)
    {
        return new IncludeApplied(fixture.Create<bool>());
    }
}
