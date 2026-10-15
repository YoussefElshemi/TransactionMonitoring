using AutoFixture;
using Core.Enums;
using Presentation.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

public static class FakeListAlertRequestDto
{
    public static ListAlertRequestDto CreateValid(IFixture fixture)
    {
        return new ListAlertRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            FriendlyName = fixture.Create<string>(),
            Status = fixture.Create<AlertStatus>().ToString(),
            Severity = fixture.Create<RuleSeverity>().ToString(),
            RuleId = fixture.Create<Guid>(),
            PartyId = fixture.Create<Guid>(),
            CaseId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
