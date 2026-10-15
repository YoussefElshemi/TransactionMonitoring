using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeEmail
{
    public static Email CreateValid(IFixture fixture)
    {
        return new Email($"{fixture.Create<string>()}@mail.com");
    }
}