namespace Core.Exceptions;

public class UniqueConstraintViolationException(string? constraintName, Exception innerException)
    : Exception($"A unique constraint was violated: '{constraintName ?? "unknown"}'.", innerException)
{
    public string? ConstraintName { get; } = constraintName;
}
