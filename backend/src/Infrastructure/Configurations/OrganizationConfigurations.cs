using Core.Constants;
using Core.Enums;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class OrganizationConfigurations : IEntityTypeConfiguration<OrganizationEntity>
{
    internal static readonly OrganizationEntity DefaultOrganization = new()
    {
        OrganizationId = SystemConstants.DefaultOrganization,
        FriendlyName = "Default Organization",
        StatusId = (int)OrganizationStatus.Enabled,
        HoldWindow = null,
        HoldExpiryVerdictId = null,
        DeduplicationWindow = null,
        CaseGroupingPeriod = null,
        CaseGroupingEnabled = null,
        DeduplicationEnabled = null,
        CaseGroupingScopeId = null,
        CaseGroupingSeverityPolicyId = null,
        SandboxMonthlyCredits = null,
        MaxAlertsPerCase = null,
        DefaultLocale = null,
        DefaultTimeZone = null,
        ApplicationUsers = [],
        ApiClients = [],
        CreatedAt = default,
        UpdatedAt = default,
        IsDeleted = false,
        DeletedAt = null
    };

    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.HasKey(x => x.OrganizationId);

        builder.HasOne(x => x.CaseGroupingScopeEntity).WithMany(x => x.Organizations).HasForeignKey(x => x.CaseGroupingScopeId);
        builder.HasOne(x => x.CaseGroupingSeverityPolicyEntity).WithMany(x => x.Organizations).HasForeignKey(x => x.CaseGroupingSeverityPolicyId);

        builder.HasMany(x => x.ApplicationUsers).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.ApiClients).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.Invoices).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.Rules).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.RuleSets).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.Usages).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.Cases).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.WebhookConfigurations).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasMany(x => x.CaseNotes).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Organizations).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.HoldExpiryVerdictEntity).WithMany(x => x.Organizations).HasForeignKey(x => x.HoldExpiryVerdictId);

        builder.Property(x => x.DefaultLocale).HasMaxLength(Locale.MaxLength);
        builder.Property(x => x.DefaultTimeZone).HasMaxLength(DisplayTimeZone.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(DefaultOrganization);

        builder.ToTable(TableNames.Organizations, Schemas.Service);
    }
}