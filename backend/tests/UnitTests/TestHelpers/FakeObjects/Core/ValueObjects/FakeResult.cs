using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeResult
{
    public static Result CreateValid(IFixture fixture)
    {
        return new Result(fixture.Create<bool>());
    }
}