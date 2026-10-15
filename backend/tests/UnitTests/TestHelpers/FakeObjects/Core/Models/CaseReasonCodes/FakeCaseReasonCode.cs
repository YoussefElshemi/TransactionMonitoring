using AutoFixture;
using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.CaseReasonCodes;

public static class FakeCaseReasonCode
{
    public static CaseReasonCode CreateValid(IFixture fixture)
    {
        return new CaseReasonCode
        {
            Mode = Mode.Live,
            CaseReasonCodeId = FakeCaseReasonCodeId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Code = new CaseReasonCodeName("MULE_ACCOUNT"),
            Description = new CaseReasonCodeDescription(fixture.Create<string>()),
            Disposition = fixture.Create<CaseDisposition>(),
            IsEnabled = new IsEnabled(true),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
