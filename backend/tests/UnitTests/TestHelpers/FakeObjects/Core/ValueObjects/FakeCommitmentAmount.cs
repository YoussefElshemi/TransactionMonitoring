using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCommitmentAmount
{
    public static CommitmentAmount CreateValid(IFixture fixture)
    {
        return new CommitmentAmount(fixture.Create<int>());
    }
}
