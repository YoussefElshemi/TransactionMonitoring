using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ProbeSourceEntityConfiguration : IEntityTypeConfiguration<ProbeSourceEntity>
{
    public void Configure(EntityTypeBuilder<ProbeSourceEntity> builder)
    {
        builder.HasKey(x => x.ProbeSourceId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.ProbeSources);

        builder.ToTable(TableNames.ProbeSources, Schemas.Service);
    }
}
