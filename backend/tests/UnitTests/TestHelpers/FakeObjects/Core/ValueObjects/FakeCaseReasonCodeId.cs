using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCaseReasonCodeId
{
    public static CaseReasonCodeId CreateValid(IFixture fixture)
    {
        return new CaseReasonCodeId(fixture.Create<Guid>());
    }
}
