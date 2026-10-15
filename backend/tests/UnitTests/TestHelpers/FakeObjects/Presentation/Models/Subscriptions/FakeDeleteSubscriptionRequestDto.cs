using AutoFixture;
using Presentation.Models.Subscriptions;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

public static class FakeDeleteSubscriptionRequestDto
{
    public static DeleteSubscriptionRequestDto CreateValid(IFixture fixture)
    {
        return new DeleteSubscriptionRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            SubscriptionId = fixture.Create<Guid>()
        };
    }
}
