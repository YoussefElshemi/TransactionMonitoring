using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreateCase
{
    public static CreateCase CreateValid(IFixture fixture)
    {
        return new CreateCase(fixture.Create<bool>());
    }
}