using Core.Enums;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using Presentation.Models.PartyMappings;

namespace Presentation.Mappers.PartyMappings;

public static class PartyRoleMappingDtoMapper
{
    public static List<PartyRoleMappingDto> ToDto(this List<PartyRoleMapping> mappings)
    {
        return mappings.ConvertAll(x => new PartyRoleMappingDto
        {
            Role = x.Role.ToString(),
            Path = x.Path?.ToString(),
            IdentifierPath = x.IdentifierPath,
            DisplayNamePath = x.DisplayNamePath?.ToString(),
            AttributePaths = x.AttributePaths?.ConvertAll(attribute => new PartyAttributeMappingDto
            {
                Name = attribute.Name,
                Path = attribute.Path
            })
        });
    }

    public static List<PartyRoleMapping> ToModel(this List<PartyRoleMappingDto> mappings)
    {
        return mappings.ConvertAll(x => new PartyRoleMapping
        {
            Role = Enum.Parse<PartyRole>(x.Role, true),
            Path = string.IsNullOrWhiteSpace(x.Path) ? null : new PartyPath(x.Path),
            IdentifierPath = new PartyPath(x.IdentifierPath),
            DisplayNamePath = string.IsNullOrWhiteSpace(x.DisplayNamePath) ? null : new PartyPath(x.DisplayNamePath),
            AttributePaths = x.AttributePaths?.ConvertAll(attribute => new PartyAttributeMapping
            {
                Name = new PartyAttributeName(attribute.Name),
                Path = new PartyPath(attribute.Path)
            })
        });
    }
}
