using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeBackTestResultId
{
    public static BackTestResultId CreateValid(IFixture fixture)
    {
        return new BackTestResultId(fixture.Create<Guid>());
    }
}
