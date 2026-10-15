namespace Core.Enums;

// Persisted as a foreign key from RoleAssignments: never renumber.
public enum PermissionScopeType
{
    Organization = 1,
    SubOrganization = 2,
    RuleSet = 3,
    Queue = 4
}
