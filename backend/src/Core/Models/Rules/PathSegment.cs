namespace Core.Models.Rules;

public record PathSegment
{
    public required string Name { get; init; }

    public required List<PathAccessor> Accessors { get; init; }
}
