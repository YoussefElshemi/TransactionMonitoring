using AutoFixture;
using Core.Enums;
using Presentation.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

public static class FakeListMyCaseRequestDto
{
    public static ListMyCaseRequestDto CreateValid(IFixture fixture)
    {
        return new ListMyCaseRequestDto
        {
            PageNumber = FakePageNumber.Valid,
            PageSize = FakePageSize.Valid,
            FromDate = FakeDateRange.Valid,
            ToDate = FakeDateRange.Valid,
            FriendlyName = fixture.Create<string>(),
            Status = CaseStatus.Assigned.ToString(),
            AssignedToUserId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}