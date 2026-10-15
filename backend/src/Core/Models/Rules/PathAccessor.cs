namespace Core.Models.Rules;

public record PathAccessor
{
    public required int? Index { get; init; }

    public required bool IsWildcard { get; init; }
}
