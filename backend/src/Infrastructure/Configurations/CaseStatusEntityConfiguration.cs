using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseStatusEntityConfiguration : IEntityTypeConfiguration<CaseStatusEntity>
{
    public void Configure(EntityTypeBuilder<CaseStatusEntity> builder)
    {
        builder.HasKey(x => x.CaseStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.CaseStatuses);

        builder.ToTable(TableNames.CaseStatuses, Schemas.Service);
    }
}