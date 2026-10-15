using AutoFixture;
using Presentation.Models.OrganizationInvoices;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

public static class FakeOrganizationInvoiceRequestDto
{
    public static OrganizationInvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new OrganizationInvoiceRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            InvoiceId = fixture.Create<Guid>()
        };
    }
}