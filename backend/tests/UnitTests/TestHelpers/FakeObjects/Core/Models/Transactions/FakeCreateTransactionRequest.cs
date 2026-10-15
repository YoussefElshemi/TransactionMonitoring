using AutoFixture;
using Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

public static class FakeCreateTransactionRequest
{
    public static CreateTransactionRequest CreateValid(IFixture fixture)
    {
        return new CreateTransactionRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture)
        };
    }
}
