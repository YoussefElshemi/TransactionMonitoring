using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeEffectiveTo
{
    public static EffectiveTo CreateValid(IFixture fixture)
    {
        return new EffectiveTo(fixture.Create<DateTimeOffset>());
    }
}
