using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.TransactionSchemas;

public record ListTransactionSchemaVersionRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
