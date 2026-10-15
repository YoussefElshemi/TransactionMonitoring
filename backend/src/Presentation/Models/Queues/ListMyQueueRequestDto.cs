using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Queues;

public record ListMyQueueRequestDto : ListQueueRequestDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid? MemberUserId
    {
        get => base.MemberUserId;
        set => base.MemberUserId = value;
    }
}
