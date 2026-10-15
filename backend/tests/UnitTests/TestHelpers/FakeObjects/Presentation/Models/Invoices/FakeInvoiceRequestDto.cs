using AutoFixture;
using Presentation.Models.Invoices;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Invoices;

public static class FakeInvoiceRequestDto
{
    public static InvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new InvoiceRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            InvoiceId = fixture.Create<Guid>()
        };
    }
}