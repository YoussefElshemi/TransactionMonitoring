using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Cases;

public record ListMyCaseRequestDto : ListCaseRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid? AssignedToUserId
    {
        get => base.AssignedToUserId;
        set => base.AssignedToUserId = value;
    }
}