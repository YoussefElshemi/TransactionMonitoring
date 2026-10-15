using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public class PartyMappingSchemaViolationException(IReadOnlyList<SchemaViolation> violations)
    : SchemaViolationException(
        $"The party mapping does not match the organisation's transaction schema. {Summarise(violations)}",
        violations)
{
    public override string GetTitle()
    {
        return "Party mapping does not match the transaction schema";
    }
}
