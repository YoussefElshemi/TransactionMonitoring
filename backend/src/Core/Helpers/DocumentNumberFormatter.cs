using System.Globalization;
using Core.ValueObjects;

namespace Core.Helpers;

public static class DocumentNumberFormatter
{
    private const int PositionDigits = 6;

    public static InvoiceNumber Invoice(NumberPrefix prefix, Year year, int position)
    {
        return new InvoiceNumber(Compose(prefix, year, position));
    }

    public static CreditNoteNumber CreditNote(NumberPrefix prefix, Year year, int position)
    {
        return new CreditNoteNumber(Compose(prefix, year, position));
    }

    public static ReceiptNumber Receipt(NumberPrefix prefix, Year year, int position)
    {
        return new ReceiptNumber(Compose(prefix, year, position));
    }

    private static string Compose(NumberPrefix prefix, Year year, int position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(string)prefix}-{(int)year:D4}-{position.ToString("D" + PositionDigits, CultureInfo.InvariantCulture)}");
    }
}
