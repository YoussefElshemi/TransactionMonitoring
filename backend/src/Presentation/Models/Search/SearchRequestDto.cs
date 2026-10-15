using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Search;

public record SearchRequestDto
{
    public string? Query { get; init; }

    public string[]? Types { get; init; }

    public int Limit { get; init; } = 5;

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
