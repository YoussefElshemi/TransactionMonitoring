using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseDispositionEntityConfiguration : IEntityTypeConfiguration<CaseDispositionEntity>
{
    public void Configure(EntityTypeBuilder<CaseDispositionEntity> builder)
    {
        builder.HasKey(x => x.CaseDispositionId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.CaseDispositions);

        builder.ToTable(TableNames.CaseDispositions, Schemas.Service);
    }
}
