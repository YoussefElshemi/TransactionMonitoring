using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApprovalStatusEntityConfiguration : IEntityTypeConfiguration<ApprovalStatusEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalStatusEntity> builder)
    {
        builder.HasKey(x => x.ApprovalStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.ApprovalStatuses);

        builder.ToTable(TableNames.ApprovalStatuses, Schemas.Service);
    }
}
