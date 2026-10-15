using AutoFixture;
using Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

public static class FakePlan
{
    public static Plan CreateValid(IFixture fixture)
    {
        return new Plan
        {
            PlanId = FakePlanId.CreateValid(fixture),
            Name = FakePlanName.CreateValid(fixture),
            Description = FakePlanDescription.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            CurrentVersionNumber = FakeVersionNumber.CreateValid(fixture),
            CurrentVersion = FakePlanVersion.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture)
        };
    }
}
