using AutoFixture;
using Presentation.Models.OrganizationInvoices;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

public static class FakeRecoverOrganizationInvoiceRequestDto
{
    public static RecoverOrganizationInvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverOrganizationInvoiceRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            InvoiceId = fixture.Create<Guid>()
        };
    }
}