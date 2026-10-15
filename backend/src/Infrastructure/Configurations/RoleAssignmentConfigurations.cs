using Core.Enums;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RoleAssignmentConfigurations : IEntityTypeConfiguration<RoleAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<RoleAssignmentEntity> builder)
    {
        builder.HasKey(x => x.RoleAssignmentId);

        builder.HasOne(x => x.User).WithMany(x => x.RoleAssignments).HasForeignKey(x => x.UserId).IsRequired();
        builder.HasOne(x => x.Role).WithMany(x => x.RoleAssignments).HasForeignKey(x => x.RoleId).IsRequired();
        builder.HasOne(x => x.ScopeTypeEntity).WithMany(x => x.RoleAssignments).HasForeignKey(x => x.ScopeTypeId).IsRequired();

        builder.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();

        builder.HasOne(x => x.SubOrganization).WithMany(x => x.RoleAssignments).HasForeignKey(x => x.SubOrganizationId);
        builder.HasOne(x => x.RuleSet).WithMany().HasForeignKey(x => x.RuleSetId);
        builder.HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId);

        builder.ToTable(TableNames.RoleAssignments, Schemas.Identity, table =>
            table.HasCheckConstraint(
                "CK_RoleAssignments_ScopeMatchesType",
                $"""
                 ("ScopeTypeId" = {(int)PermissionScopeType.Organization} AND "SubOrganizationId" IS NULL AND "RuleSetId" IS NULL AND "QueueId" IS NULL)
                 OR ("ScopeTypeId" = {(int)PermissionScopeType.SubOrganization} AND "SubOrganizationId" IS NOT NULL AND "RuleSetId" IS NULL AND "QueueId" IS NULL)
                 OR ("ScopeTypeId" = {(int)PermissionScopeType.RuleSet} AND "SubOrganizationId" IS NULL AND "RuleSetId" IS NOT NULL AND "QueueId" IS NULL)
                 OR ("ScopeTypeId" = {(int)PermissionScopeType.Queue} AND "SubOrganizationId" IS NULL AND "RuleSetId" IS NULL AND "QueueId" IS NOT NULL)
                 """));

        // Four partial indexes because Postgres treats nulls as distinct, so one index over all six columns would admit duplicate org-wide grants.
        builder.HasIndex(x => new { x.UserId, x.RoleId })
            .IsUnique()
            .HasFilter($"\"ScopeTypeId\" = {(int)PermissionScopeType.Organization}")
            .HasDatabaseName("IX_RoleAssignments_Unique_Organization");

        builder.HasIndex(x => new { x.UserId, x.RoleId, x.SubOrganizationId })
            .IsUnique()
            .HasFilter($"\"ScopeTypeId\" = {(int)PermissionScopeType.SubOrganization}")
            .HasDatabaseName("IX_RoleAssignments_Unique_SubOrganization");

        builder.HasIndex(x => new { x.UserId, x.RoleId, x.RuleSetId })
            .IsUnique()
            .HasFilter($"\"ScopeTypeId\" = {(int)PermissionScopeType.RuleSet}")
            .HasDatabaseName("IX_RoleAssignments_Unique_RuleSet");

        builder.HasIndex(x => new { x.UserId, x.RoleId, x.QueueId })
            .IsUnique()
            .HasFilter($"\"ScopeTypeId\" = {(int)PermissionScopeType.Queue}")
            .HasDatabaseName("IX_RoleAssignments_Unique_Queue");

        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
