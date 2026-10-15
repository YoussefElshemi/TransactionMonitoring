using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeRuleSetEntity
{
    public static RuleSetEntity CreateValid(IFixture fixture)
    {
        return new RuleSetEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            RuleSetId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            IsEnabled = fixture.Create<bool>(),
            ReviewThreshold = fixture.Create<int>() % 101,
            SubOrganizationId = null,
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture)
        };
    }
}