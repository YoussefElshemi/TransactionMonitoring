using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIncludeDisabled
{
    public static IncludeDisabled CreateValid(IFixture fixture)
    {
        return new IncludeDisabled(fixture.Create<bool>());
    }
}
