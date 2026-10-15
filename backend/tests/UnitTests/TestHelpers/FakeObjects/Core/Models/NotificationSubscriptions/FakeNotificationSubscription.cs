using AutoFixture;
using Core.Enums;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.NotificationSubscriptions;

public static class FakeNotificationSubscription
{
    public static NotificationSubscription CreateValid(IFixture fixture)
    {
        return new NotificationSubscription
        {
            Mode = Mode.Live,
            NotificationSubscriptionId = new NotificationSubscriptionId(fixture.Create<Guid>()),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Channel = NotificationChannel.Email,
            Destination = new Destination("ops@mail.com"),
            MinimumSeverity = null,
            NotifyOnBreach = new NotifyOnBreach(false),
            IsEnabled = new IsEnabled(true),
            Locale = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
