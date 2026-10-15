using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeApiClientSecretId
{
    public static ApiClientSecretId CreateValid(IFixture fixture)
    {
        return new ApiClientSecretId(fixture.Create<Guid>());
    }
}
