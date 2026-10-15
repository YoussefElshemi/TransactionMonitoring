namespace Presentation.Models.Errors;

public record ErrorDefinitionResponseDto
{
    public required string Code { get; init; }
    public required int Status { get; init; }
    public required string Description { get; init; }
}
