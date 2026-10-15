using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreditNoteLineId
{
    public static CreditNoteLineId CreateValid(IFixture fixture)
    {
        return new CreditNoteLineId(fixture.Create<Guid>());
    }
}
