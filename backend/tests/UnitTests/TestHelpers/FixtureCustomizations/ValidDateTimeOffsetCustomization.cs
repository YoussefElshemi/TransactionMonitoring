using AutoFixture;

namespace UnitTests.TestHelpers.FixtureCustomizations;

public class ValidDateTimeOffsetCustomization : ICustomization
{
    private static readonly DateTimeOffset StartDate = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndDate = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Customize(IFixture fixture)
    {
        fixture.Customize<DateTimeOffset>(composer => composer.FromFactory(() =>
        {
            var random = new Random();
            var range = EndDate - StartDate;
            var randTimeSpan = new TimeSpan((long)(random.NextDouble() * range.Ticks));

            return StartDate.Add(randTimeSpan);
        }));
    }
}
