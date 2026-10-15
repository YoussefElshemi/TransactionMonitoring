using Core.Interfaces.Repositories;
using Core.ValueObjects;
using Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

// Raw upsert: atomic and inside the caller's transaction, so a rollback returns the position. nextval would leave gaps.
public class DocumentNumberRepository(TransactionMonitoringDbContext context) : IDocumentNumberRepository
{
    public Task<int> NextInvoicePositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken)
    {
        return NextAsync(billingEntityId, year, "LastInvoiceNumber", stoppingToken);
    }

    public Task<int> NextCreditNotePositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken)
    {
        return NextAsync(billingEntityId, year, "LastCreditNoteNumber", stoppingToken);
    }

    public Task<int> NextReceiptPositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken)
    {
        return NextAsync(billingEntityId, year, "LastReceiptNumber", stoppingToken);
    }

    private async Task<int> NextAsync(BillingEntityId billingEntityId, Year year, string column, CancellationToken stoppingToken)
    {
        var sql =
            $"""
             INSERT INTO "{Schemas.Service}"."{TableNames.InvoiceNumberSequences}"
                 ("BillingEntityId", "Year", "LastInvoiceNumber", "LastCreditNoteNumber", "LastReceiptNumber", "CreatedAt", "UpdatedAt")
             VALUES (@billingEntityId, @year, {Seed(column)}, now(), now())
             ON CONFLICT ("BillingEntityId", "Year") DO UPDATE
                 SET "{column}" = "{TableNames.InvoiceNumberSequences}"."{column}" + 1,
                     "UpdatedAt" = now()
             RETURNING "{column}" as "Value"
             """;

        var positions = await context.Database
            .SqlQueryRaw<int>(sql,
                new NpgsqlParameter("billingEntityId", (Guid)billingEntityId),
                new NpgsqlParameter("year", (int)year))
            .ToListAsync(stoppingToken);

        return positions[0];
    }

    // Values are positional in the INSERT; keep in step with the column list.
    private static string Seed(string column)
    {
        return string.Join(", ", Columns.Select(x => x == column ? "1" : "0"));
    }

    private static readonly string[] Columns = ["LastInvoiceNumber", "LastCreditNoteNumber", "LastReceiptNumber"];
}
