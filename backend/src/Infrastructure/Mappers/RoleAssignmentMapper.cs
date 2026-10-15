using Core.Enums;
using Core.Models.Permissions;
using Core.Models.Roles;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RoleAssignmentMapper
{
    public static RoleAssignment ToModel(this RoleAssignmentEntity roleAssignmentEntity)
    {
        return new RoleAssignment
        {
            RoleAssignmentId = new RoleAssignmentId(roleAssignmentEntity.RoleAssignmentId),
            UserId = new ApplicationUserId(roleAssignmentEntity.UserId),
            RoleId = new RoleId(roleAssignmentEntity.RoleId),
            Role = roleAssignmentEntity.Role is not null ? roleAssignmentEntity.Role.ToModel() : null,
            Scope = new PermissionScope
            {
                ScopeType = (PermissionScopeType)roleAssignmentEntity.ScopeTypeId,
                SubOrganizationId = roleAssignmentEntity.SubOrganizationId.HasValue ? new SubOrganizationId(roleAssignmentEntity.SubOrganizationId.Value) : null,
                RuleSetId = roleAssignmentEntity.RuleSetId.HasValue ? new RuleSetId(roleAssignmentEntity.RuleSetId.Value) : null,
                QueueId = roleAssignmentEntity.QueueId.HasValue ? new QueueId(roleAssignmentEntity.QueueId.Value) : null
            },
            AssignedByUserId = new ApplicationUserId(roleAssignmentEntity.AssignedByUserId),
            CreatedAt = new CreatedAt(roleAssignmentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(roleAssignmentEntity.UpdatedAt)
        };
    }

    public static RoleAssignmentEntity ToEntity(this RoleAssignment roleAssignment)
    {
        return new RoleAssignmentEntity
        {
            RoleAssignmentId = roleAssignment.RoleAssignmentId,
            UserId = roleAssignment.UserId,
            RoleId = roleAssignment.RoleId,
            ScopeTypeId = (int)roleAssignment.Scope.ScopeType,
            SubOrganizationId = roleAssignment.Scope.SubOrganizationId.HasValue ? (Guid)roleAssignment.Scope.SubOrganizationId.Value : null,
            RuleSetId = roleAssignment.Scope.RuleSetId.HasValue ? (Guid)roleAssignment.Scope.RuleSetId.Value : null,
            QueueId = roleAssignment.Scope.QueueId.HasValue ? (Guid)roleAssignment.Scope.QueueId.Value : null,
            AssignedByUserId = roleAssignment.AssignedByUserId,
            CreatedAt = roleAssignment.CreatedAt,
            UpdatedAt = roleAssignment.UpdatedAt
        };
    }
}
