using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ModeEntityConfiguration : IEntityTypeConfiguration<ModeEntity>
{
    public void Configure(EntityTypeBuilder<ModeEntity> builder)
    {
        builder.HasKey(x => x.ModeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.Modes);

        builder.ToTable(TableNames.Modes, Schemas.Service);
    }
}
