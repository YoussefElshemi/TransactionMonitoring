using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeApiClientId
{
    public static ApiClientId CreateValid(IFixture fixture)
    {
        return new ApiClientId(fixture.Create<Guid>());
    }
}
