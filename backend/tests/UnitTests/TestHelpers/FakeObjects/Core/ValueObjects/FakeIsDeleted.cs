using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIsDeleted
{
    public static IsDeleted CreateValid(IFixture fixture)
    {
        return new IsDeleted(fixture.Create<bool>());
    }
}