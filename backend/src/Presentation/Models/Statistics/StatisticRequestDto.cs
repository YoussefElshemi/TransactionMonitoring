using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Statistics;

public record StatisticRequestDto : DatedRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public required Guid OrganizationId { get; set; }
}