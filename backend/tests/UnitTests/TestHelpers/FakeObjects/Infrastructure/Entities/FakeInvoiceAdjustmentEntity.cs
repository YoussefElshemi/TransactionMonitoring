using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeInvoiceAdjustmentEntity
{
    public static InvoiceAdjustmentEntity CreateValid(IFixture fixture)
    {
        return new InvoiceAdjustmentEntity
        {
            InvoiceAdjustmentId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Month = 6,
            Year = 2026,
            Description = fixture.Create<string>(),
            Amount = -250.50m,
            AppliedToInvoiceId = fixture.Create<Guid>(),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
