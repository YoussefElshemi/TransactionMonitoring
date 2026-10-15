using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCaseNoteId
{
    public static CaseNoteId CreateValid(IFixture fixture)
    {
        return new CaseNoteId(fixture.Create<Guid>());
    }
}