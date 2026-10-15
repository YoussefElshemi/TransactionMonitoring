using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeEffectiveFrom
{
    public static EffectiveFrom CreateValid(IFixture fixture)
    {
        return new EffectiveFrom(fixture.Create<DateTimeOffset>());
    }
}
