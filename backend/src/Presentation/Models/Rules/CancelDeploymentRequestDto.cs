using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Rules;

public record CancelDeploymentRequestDto : RuleRequestDto
{
    [FromRoute]
    public required Guid DeploymentId { get; init; }
}
