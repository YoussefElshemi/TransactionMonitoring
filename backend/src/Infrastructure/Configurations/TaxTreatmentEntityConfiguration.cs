using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TaxTreatmentEntityConfiguration : IEntityTypeConfiguration<TaxTreatmentEntity>
{
    public void Configure(EntityTypeBuilder<TaxTreatmentEntity> builder)
    {
        builder.HasKey(x => x.TaxTreatmentId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.TaxTreatments);

        builder.ToTable(TableNames.TaxTreatments, Schemas.Service);
    }
}
