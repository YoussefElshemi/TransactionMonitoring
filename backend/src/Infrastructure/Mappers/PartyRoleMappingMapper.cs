using Core.Extensions;
using Core.Models.PartyMappings;
using Core.ValueObjects;

namespace Infrastructure.Mappers;

public static class PartyRoleMappingMapper
{
    public static List<PartyRoleMapping> ToModel(this List<PartyRoleMappingModel> mappings)
    {
        return mappings.ConvertAll(x => new PartyRoleMapping
        {
            Role = x.Role,
            Path = string.IsNullOrWhiteSpace(x.Path) ? null : new PartyPath(x.Path),
            IdentifierPath = new PartyPath(x.IdentifierPath ?? string.Empty),
            DisplayNamePath = string.IsNullOrWhiteSpace(x.DisplayNamePath) ? null : new PartyPath(x.DisplayNamePath),
            AttributePaths = x.AttributePaths?.ConvertAll(attribute => new PartyAttributeMapping
            {
                Name = new PartyAttributeName(attribute.Name ?? string.Empty),
                Path = new PartyPath(attribute.Path ?? string.Empty)
            })
        });
    }

    public static List<PartyRoleMapping> MappingsToModel(this string mappings)
    {
        return mappings.FromBase64String().TryDeserialize<List<PartyRoleMappingModel>>().ToModel();
    }

    public static string ToBase64String(this List<PartyRoleMapping> mappings)
    {
        return mappings.ToEntity().Serialize().ToBase64String();
    }

    private static List<PartyRoleMappingModel> ToEntity(this List<PartyRoleMapping> mappings)
    {
        return mappings.ConvertAll(x => new PartyRoleMappingModel
        {
            Role = x.Role,
            Path = x.Path?.ToString(),
            IdentifierPath = x.IdentifierPath,
            DisplayNamePath = x.DisplayNamePath?.ToString(),
            AttributePaths = x.AttributePaths?.ConvertAll(attribute => new PartyAttributeMappingModel
            {
                Name = attribute.Name,
                Path = attribute.Path
            })
        });
    }
}
