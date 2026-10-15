using AutoFixture;
using Presentation.Models.OrganizationInvoices;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationInvoices;

public static class FakeGenerateOrganizationInvoiceRequestDto
{
    public static GenerateOrganizationInvoiceRequestDto CreateValid(IFixture fixture)
    {
        return new GenerateOrganizationInvoiceRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new GenerateOrganizationInvoiceRequestBodyDto
            {

                Month = fixture.Create<int>(),
                Year = fixture.Create<int>()
            }
        };
    }
}