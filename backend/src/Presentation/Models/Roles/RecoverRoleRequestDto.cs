namespace Presentation.Models.Roles;

// Separate type: recovery must see deleted roles, which every other lookup refuses.
public record RecoverRoleRequestDto : OrganizationRoleRequestDto;
