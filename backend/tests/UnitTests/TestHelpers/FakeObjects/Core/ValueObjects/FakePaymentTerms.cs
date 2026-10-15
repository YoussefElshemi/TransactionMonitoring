using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePaymentTerms
{
    public static PaymentTerms CreateValid(IFixture fixture)
    {
        return new PaymentTerms(28);
    }
}
