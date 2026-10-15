using AutoFixture;
using Core.Models.Common;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Common;

public static class FakePagedResponse
{
    public static PagedResponse<T> CreateValid<T>(IFixture fixture, Func<T> func)
    {
        return new PagedResponse<T>
        {
            PageNumber = FakePageNumber.CreateValid(fixture),
            PageSize = FakePageSize.CreateValid(fixture),
            PageCount = FakePageCount.CreateValid(fixture),
            TotalCount = FakeTotalCount.CreateValid(fixture),
            HasMore = FakeHasMore.CreateValid(fixture),
            Data = Enumerable.Range(0, 10).Select(_ => func()).ToList()
        };
    }
}