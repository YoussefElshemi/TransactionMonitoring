using Core.Interfaces.Repositories;
using Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

public class SandboxCreditRepository(TransactionMonitoringDbContext context) : ISandboxCreditRepository
{
    // A different class id from the audit chain's lock so the two never contend.
    private const int ClassId = 4506;

    public Task LockAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        // xact, not session: released on rollback too.
        return context.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({ClassId}, hashtext(@organizationId))",
            [new NpgsqlParameter("organizationId", ((Guid)organizationId).ToString())],
            stoppingToken);
    }
}
