using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreditNoteId
{
    public static CreditNoteId CreateValid(IFixture fixture)
    {
        return new CreditNoteId(fixture.Create<Guid>());
    }
}
