using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeFreeAllowance
{
    public static FreeAllowance CreateValid(IFixture fixture)
    {
        return new FreeAllowance(fixture.Create<int>());
    }
}
