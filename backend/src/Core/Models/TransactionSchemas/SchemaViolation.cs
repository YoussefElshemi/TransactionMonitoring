namespace Core.Models.TransactionSchemas;

public record SchemaViolation
{
    public required string Field { get; init; }
    public required string Message { get; init; }
}
