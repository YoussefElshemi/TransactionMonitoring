using AutoFixture;
using Presentation.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Transactions;

public static class FakeListTransactionRequestDto
{
    public static ListTransactionRequestDto CreateValid(IFixture fixture)
    {
        return new ListTransactionRequestDto
        {
            FriendlyName = fixture.Create<string>(),
            ExternalReference = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid
        };
    }
}
