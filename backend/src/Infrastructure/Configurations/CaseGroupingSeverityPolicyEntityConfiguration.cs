using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseGroupingSeverityPolicyEntityConfiguration : IEntityTypeConfiguration<CaseGroupingSeverityPolicyEntity>
{
    public void Configure(EntityTypeBuilder<CaseGroupingSeverityPolicyEntity> builder)
    {
        builder.HasKey(x => x.CaseGroupingSeverityPolicyId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.CaseGroupingSeverityPolicies);

        builder.ToTable(TableNames.CaseGroupingSeverityPolicies, Schemas.Service);
    }
}
