using AutoFixture;
using Core.Enums;
using Presentation.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeListCaseRequestDto
{
    public static ListCaseRequestDto CreateValid(IFixture fixture)
    {
        return new ListCaseRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            FriendlyName = fixture.Create<string>(),
            Status = fixture.Create<CaseStatus>().ToString(),
            Severity = fixture.Create<RuleSeverity>().ToString(),
            AssignedToUserId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}