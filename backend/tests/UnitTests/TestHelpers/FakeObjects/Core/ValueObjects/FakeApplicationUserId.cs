using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeApplicationUserId
{
    public static ApplicationUserId CreateValid(IFixture fixture)
    {
        return new ApplicationUserId(fixture.Create<Guid>());
    }
}