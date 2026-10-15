using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreditNoteNumber
{
    public static CreditNoteNumber CreateValid(IFixture fixture)
    {
        return new CreditNoteNumber("CRN-2026-000001");
    }
}
