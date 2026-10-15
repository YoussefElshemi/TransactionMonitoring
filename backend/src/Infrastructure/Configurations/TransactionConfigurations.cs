using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionConfigurations : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.HasKey(x => x.TransactionId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Transactions).HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId, x.ExternalReference }).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.FriendlyName).IsUnique();


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Transactions, Schemas.Service);
    }
}
