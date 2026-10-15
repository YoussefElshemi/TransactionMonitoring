using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionPartyConfigurations : IEntityTypeConfiguration<TransactionPartyEntity>
{
    public void Configure(EntityTypeBuilder<TransactionPartyEntity> builder)
    {
        builder.HasKey(x => new { x.TransactionId, x.PartyId, x.PartyRoleId });

        builder.HasOne(x => x.Transaction).WithMany(x => x.TransactionParties).HasForeignKey(x => x.TransactionId).IsRequired();
        builder.HasOne(x => x.Party).WithMany(x => x.TransactionParties).HasForeignKey(x => x.PartyId).IsRequired();
        builder.HasOne(x => x.PartyRoleEntity).WithMany(x => x.TransactionParties).HasForeignKey(x => x.PartyRoleId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.TransactionParties, Schemas.Service);
    }
}
