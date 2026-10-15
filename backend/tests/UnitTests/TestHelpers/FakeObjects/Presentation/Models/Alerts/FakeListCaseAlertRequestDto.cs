using AutoFixture;
using Core.Enums;
using Presentation.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

public static class FakeListCaseAlertRequestDto
{
    public static ListCaseAlertRequestDto CreateValid(IFixture fixture)
    {
        var routeCaseId = fixture.Create<Guid>();

        return new ListCaseAlertRequestDto
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
            RouteCaseId = routeCaseId,
            CaseId = routeCaseId,
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
