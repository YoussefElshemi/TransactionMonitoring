using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeUsageId
{
    public static UsageId CreateValid(IFixture fixture)
    {
        return new UsageId(fixture.Create<Guid>());
    }
}