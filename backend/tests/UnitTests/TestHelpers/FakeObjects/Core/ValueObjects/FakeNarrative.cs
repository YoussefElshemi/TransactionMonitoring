using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeNarrative
{
    public static Narrative CreateValid(IFixture fixture)
    {
        return new Narrative(fixture.Create<string>());
    }
}
