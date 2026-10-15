using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCaseId
{
    public static CaseId CreateValid(IFixture fixture)
    {
        return new CaseId(fixture.Create<Guid>());
    }
}