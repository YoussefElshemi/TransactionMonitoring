using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeApprovalId
{
    public static ApprovalId CreateValid(IFixture fixture)
    {
        return new ApprovalId(fixture.Create<Guid>());
    }
}
