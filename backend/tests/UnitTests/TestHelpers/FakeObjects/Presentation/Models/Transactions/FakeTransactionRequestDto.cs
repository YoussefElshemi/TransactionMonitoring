using AutoFixture;
using Presentation.Models.Transactions;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Transactions;

public static class FakeTransactionRequestDto
{
    public static TransactionRequestDto CreateValid(IFixture fixture)
    {
        return new TransactionRequestDto
        {
            TransactionId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
