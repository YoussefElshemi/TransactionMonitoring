using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreditNoteReason
{
    public static CreditNoteReason CreateValid(IFixture fixture)
    {
        return new CreditNoteReason(fixture.Create<string>());
    }
}
