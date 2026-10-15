using System.Net;
using Core.Models.TransactionSchemas;

namespace Core.Exceptions;

public abstract class SchemaViolationException(string message, IReadOnlyList<SchemaViolation> violations)
    : DomainException(message)
{
    public IReadOnlyList<SchemaViolation> Violations { get; } = violations;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    // Full detail including values; reaches only the caller, unlike Message which is recorded on the span.
    public override object GetExtensions()
    {
        return new
        {
            Violations = Violations.Select(x => new { x.Field, x.Message }).ToList()
        };
    }

    protected static string Summarise(IReadOnlyList<SchemaViolation> violations)
    {
        return string.Join(" ", violations.Select(x => $"'{x.Field}': {x.Message}"));
    }

    // Field names only: this becomes Message, which is recorded onto the current span and exported.
    protected static string SummariseFields(IReadOnlyList<SchemaViolation> violations)
    {
        return string.Join(", ", violations.Select(x => $"'{x.Field}'"));
    }
}
