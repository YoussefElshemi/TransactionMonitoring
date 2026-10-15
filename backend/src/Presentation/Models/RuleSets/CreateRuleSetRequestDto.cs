using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.RuleSets;

public record CreateRuleSetRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }

    public required string FriendlyName { get; init; }
    public required bool IsEnabled { get; init; }
    public int? ReviewThreshold { get; init; }

    public Guid? SubOrganizationId { get; init; }
}