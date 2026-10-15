using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class VelocityRuleViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The rule does not match the organisation's velocity profile. {Summarise(violations)}",
        violations)
{
    public override string GetTitle()
    {
        return "Rule does not match the velocity profile";
    }
}
