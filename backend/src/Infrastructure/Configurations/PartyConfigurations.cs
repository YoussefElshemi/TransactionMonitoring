using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PartyConfigurations : IEntityTypeConfiguration<PartyEntity>
{
    public void Configure(EntityTypeBuilder<PartyEntity> builder)
    {
        builder.HasKey(x => x.PartyId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Parties).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.RiskSetByUser).WithMany(x => x.AssessedParties).HasForeignKey(x => x.RiskSetByUserId);

        builder.Property(x => x.Identifier).HasMaxLength(PartyIdentifier.MaxLength);
        builder.Property(x => x.DisplayName).HasMaxLength(PartyDisplayName.MaxLength);
        builder.Property(x => x.RiskNote).HasMaxLength(RiskNote.MaxLength);

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId, x.Identifier }).IsUnique();

        // Trigram index: a b-tree cannot serve "contains".
        builder.HasIndex(x => x.Identifier).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(x => x.DisplayName).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Parties, Schemas.Service);
    }
}
