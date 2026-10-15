using Core.ValueObjects;

namespace Core.Helpers;

public static class BillingPeriod
{
    public static DateTimeOffset EndOf(Month month, Year year)
    {
        var lastDay = DateTime.DaysInMonth(year, month);

        return new DateTimeOffset(year, month, lastDay, 23, 59, 59, TimeSpan.Zero);
    }

    public static int Key(Month month, Year year)
    {
        return year * 12 + month;
    }

    public static DateTimeOffset StartOf(Month month, Year year)
    {
        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static (Month Month, Year Year) FromKey(int key)
    {
        var year = (key - 1) / 12;

        return (new Month(key - year * 12), new Year(year));
    }

    public static (int FirstKey, int LastKey) KeysWithin(DateTimeOffset from, DateTimeOffset to)
    {
        var endOfFirst = EndOf(new Month(from.Month), new Year(from.Year));
        var firstKey = from.Year * 12 + from.Month + (endOfFirst < from ? 1 : 0);

        var endOfLast = EndOf(new Month(to.Month), new Year(to.Year));
        var lastKey = to.Year * 12 + to.Month - (endOfLast < to ? 0 : 1);

        return (firstKey, lastKey);
    }
}
