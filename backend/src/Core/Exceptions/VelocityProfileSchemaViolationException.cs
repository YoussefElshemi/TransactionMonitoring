using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class VelocityProfileSchemaViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The velocity profile does not match the organisation's transaction schema. {Summarise(violations)}",
        violations)
{
    public override string GetTitle()
    {
        return "Velocity profile does not match the transaction schema";
    }
}
