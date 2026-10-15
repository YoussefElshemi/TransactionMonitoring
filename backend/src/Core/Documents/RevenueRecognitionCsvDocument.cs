using System.Globalization;
using System.Text;
using Core.Helpers;
using Core.Models.Revenue;

namespace Core.Documents;

public static class RevenueRecognitionCsvDocument
{
    public const string ContentType = "text/csv";

    private const string LineSeparator = "\r\n";
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

    private static readonly string[] Headers =
    [
        "DocumentType",
        "DocumentNumber",
        "IssuedAt",
        "RecognitionPeriod",
        "BillingEntity",
        "OrganizationId",
        "CustomerName",
        "CustomerCountry",
        "Currency",
        "NetAmount",
        "TaxAmount",
        "GrossAmount",
        "TaxTreatment",
        "TaxRate",
        "ReportingCurrency",
        "ReportingRate",
        "ReportingNetAmount"
    ];

    public static byte[] Write(IReadOnlyCollection<RecognitionEntry> entries)
    {
        var builder = new StringBuilder();

        builder.Append(string.Join(',', Headers)).Append(LineSeparator);

        foreach (var entry in entries)
        {
            builder.Append(string.Join(',',
                Quote(entry.DocumentType.ToString()),
                Quote(entry.DocumentNumber.ToString()),
                Quote(((DateTimeOffset)entry.IssuedAt).ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)),

                Quote($"{(int)entry.Year:D4}-{(int)entry.Month:D2}"),
                Quote(entry.BillingEntityName?.ToString() ?? string.Empty),
                Quote(entry.OrganizationId.ToString()),
                Quote(entry.CustomerName?.ToString() ?? string.Empty),
                Quote(entry.CustomerCountryCode?.ToString() ?? string.Empty),
                Quote(CurrencyFormatter.Code(entry.Currency)),

                Quote(((decimal)entry.NetAmount).ToString(CultureInfo.InvariantCulture)),
                Quote(((decimal)entry.TaxAmount).ToString(CultureInfo.InvariantCulture)),
                Quote(((decimal)entry.Amount).ToString(CultureInfo.InvariantCulture)),
                Quote(entry.TaxTreatment.ToString()),
                Quote(((decimal)entry.TaxRate).ToString(CultureInfo.InvariantCulture)),
                Quote(CurrencyFormatter.Code(entry.ReportingCurrency)),
                Quote(((decimal)entry.ReportingRate).ToString(CultureInfo.InvariantCulture)),
                Quote(((decimal)entry.ReportingNetAmount).ToString(CultureInfo.InvariantCulture)))).Append(LineSeparator);
        }

        // GetBytes never emits a BOM; without one spreadsheets guess the code page and mangle non-ASCII names.
        var encoding = new UTF8Encoding(true);

        return [.. encoding.GetPreamble(), .. encoding.GetBytes(builder.ToString())];
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
