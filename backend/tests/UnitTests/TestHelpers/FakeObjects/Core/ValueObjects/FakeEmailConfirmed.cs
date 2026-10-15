using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeEmailConfirmed
{
    public static EmailConfirmed CreateValid(IFixture fixture)
    {
        return new EmailConfirmed(fixture.Create<bool>());
    }
}