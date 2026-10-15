using AutoFixture;
using Core.Configs;

namespace UnitTests.TestHelpers.FakeObjects.Core.Configs;

public static class FakeIdempotencyConfig
{
    public static IdempotencyConfig CreateValid(IFixture fixture)
    {
        return new IdempotencyConfig
        {
            RetentionHours = 24
        };
    }
}
