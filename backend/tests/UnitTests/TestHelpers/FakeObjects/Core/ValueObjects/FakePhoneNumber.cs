using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePhoneNumber
{
    public static PhoneNumber CreateValid(IFixture fixture)
    {
        return new PhoneNumber(fixture.Create<string>());
    }
}