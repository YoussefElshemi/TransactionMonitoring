using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCommitmentId
{
    public static CommitmentId CreateValid(IFixture fixture)
    {
        return new CommitmentId(fixture.Create<Guid>());
    }
}
