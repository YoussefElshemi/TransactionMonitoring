using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class PayloadSchemaViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The transaction payload does not match the organisation's transaction schema. Fields: {SummariseFields(violations)}.",
        violations)
{
    public override string GetTitle()
    {
        return "Transaction payload does not match the transaction schema";
    }
}
