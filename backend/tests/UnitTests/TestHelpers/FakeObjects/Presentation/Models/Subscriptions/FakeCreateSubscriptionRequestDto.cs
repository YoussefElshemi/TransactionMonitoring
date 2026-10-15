using AutoFixture;
using Presentation.Models.Subscriptions;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

public static class FakeCreateSubscriptionRequestDto
{
    public static CreateSubscriptionRequestDto CreateValid(IFixture fixture)
    {
        return new CreateSubscriptionRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateSubscriptionRequestBodyDto
            {
                PlanVersionId = fixture.Create<Guid>(),
                EffectiveFrom = null
            },
            CreatedByUserId = fixture.Create<Guid>()
        };
    }
}
