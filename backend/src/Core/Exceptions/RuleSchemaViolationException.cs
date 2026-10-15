using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class RuleSchemaViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The rule does not match the organisation's transaction schema. {Summarise(violations)}",
        violations)
{
    public override string GetTitle()
    {
        return "Rule does not match the transaction schema";
    }
}
