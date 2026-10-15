using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDateRange
{
    public static readonly DateOnly Valid = DateOnly.FromDateTime(TestTimeConstants.Today.UtcDateTime);

    public static DateRange CreateValid(IFixture fixture)
    {
        return new DateRange(Valid);
    }
}