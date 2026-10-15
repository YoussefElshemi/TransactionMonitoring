using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApprovalSubjectEntityConfiguration : IEntityTypeConfiguration<ApprovalSubjectEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalSubjectEntity> builder)
    {
        builder.HasKey(x => x.ApprovalSubjectId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.ApprovalSubjects);

        builder.ToTable(TableNames.ApprovalSubjects, Schemas.Service);
    }
}
