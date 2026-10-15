using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AccountEntryReasonEntityConfiguration : IEntityTypeConfiguration<AccountEntryReasonEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntryReasonEntity> builder)
    {
        builder.HasKey(x => x.AccountEntryReasonId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AccountEntryReasons);

        builder.ToTable(TableNames.AccountEntryReasons, Schemas.Service);
    }
}
