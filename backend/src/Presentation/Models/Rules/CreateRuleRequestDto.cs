using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Rules;

public record CreateRuleRequestDto
{
    [FromRoute]
    public required Guid RuleSetId { get; init; }

    [FromBody]
    public required CreateRuleRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record CreateRuleRequestBodyDto
{
    public required string FriendlyName { get; init; }
    public required RuleBodyDto RuleBody { get; init; }
    public required bool IsEnabled { get; init; }
    public string MatchPolarity { get; init; } = nameof(RuleMatchPolarity.MatchesWhenTrue);
    public string Action { get; init; } = nameof(RuleAction.Review);
    public string Severity { get; init; } = nameof(RuleSeverity.Medium);
    public int RiskWeight { get; init; }
    public int? HoldWindow { get; init; }
    public string? HoldExpiryVerdict { get; init; }
    public string? AlertSubjectRole { get; init; }
    public int? DeduplicationWindow { get; init; }
}