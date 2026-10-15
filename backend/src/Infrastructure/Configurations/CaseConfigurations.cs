using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseConfigurations : IEntityTypeConfiguration<CaseEntity>
{
    public void Configure(EntityTypeBuilder<CaseEntity> builder)
    {
        builder.HasKey(x => x.CaseId);

        builder.HasOne(x => x.AssignedToUser).WithMany(x => x.Cases).HasForeignKey(x => x.AssignedToUserId);
        builder.HasOne(x => x.Organization).WithMany(x => x.Cases).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Transaction).WithMany(x => x.Cases).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RuleEvaluations).WithMany(x => x.Cases).UsingEntity<CaseRuleEvaluationLinkEntity>();
        builder.HasMany(x => x.CaseNotes).WithOne(x => x.Case).HasForeignKey(x => x.CaseId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Cases).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.SeverityEntity).WithMany(x => x.Cases).HasForeignKey(x => x.SeverityId).IsRequired();
        builder.HasOne(x => x.Party).WithMany(x => x.Cases).HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DispositionEntity).WithMany(x => x.Cases).HasForeignKey(x => x.DispositionId);

        builder.HasOne(x => x.ReasonCode).WithMany(x => x.Cases).HasForeignKey(x => x.CaseReasonCodeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Queue).WithMany(x => x.Cases).HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EscalationRule).WithMany(x => x.Cases).HasForeignKey(x => x.EscalationRuleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.PartyId, x.StatusId });

        builder.HasIndex(x => new { x.QueueId, x.StatusId, x.AssignedToUserId });

        builder.HasIndex(x => new { x.OrganizationId, x.StatusId, x.DueAt });

        builder.HasIndex(x => x.FriendlyName).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.Navigation(x => x.AssignedToUser).AutoInclude();
        builder.Navigation(x => x.RuleEvaluations).AutoInclude();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Cases, Schemas.Service);
    }
}