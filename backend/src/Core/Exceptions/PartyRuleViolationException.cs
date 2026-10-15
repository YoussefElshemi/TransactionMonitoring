using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class PartyRuleViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The rule does not match the organisation's party mapping. {Summarise(violations)}",
        violations)
{
    public override string GetTitle()
    {
        return "Rule does not match the party mapping";
    }
}
