using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeResource
{
    public static Resource CreateValid(IFixture fixture)
    {
        return new Resource(fixture.Create<string>());
    }
}