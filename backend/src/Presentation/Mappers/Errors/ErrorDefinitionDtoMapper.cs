using Core.Models.Errors;
using Presentation.Models.Errors;

namespace Presentation.Mappers.Errors;

public static class ErrorDefinitionDtoMapper
{
    public static ErrorDefinitionResponseDto ToDto(this ErrorDefinition errorDefinition)
    {
        return new ErrorDefinitionResponseDto
        {
            Code = errorDefinition.Code,
            Status = errorDefinition.Status,
            Description = errorDefinition.Description
        };
    }
}
