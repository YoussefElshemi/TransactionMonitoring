using AutoFixture;
using Core.Models.Adjustments;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;

public static class FakeInvoiceAdjustment
{
    public static InvoiceAdjustment CreateValid(IFixture fixture)
    {
        return new InvoiceAdjustment
        {
            InvoiceAdjustmentId = FakeInvoiceAdjustmentId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Month = FakeMonth.CreateValid(fixture),
            Year = FakeYear.CreateValid(fixture),
            Description = FakeInvoiceAdjustmentDescription.CreateValid(fixture),
            Amount = FakeAdjustmentAmount.CreateValid(fixture),
            AppliedToInvoiceId = null,
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
