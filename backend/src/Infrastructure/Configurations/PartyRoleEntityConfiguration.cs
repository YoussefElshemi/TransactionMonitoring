using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PartyRoleEntityConfiguration : IEntityTypeConfiguration<PartyRoleEntity>
{
    public void Configure(EntityTypeBuilder<PartyRoleEntity> builder)
    {
        builder.HasKey(x => x.PartyRoleId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.PartyRoles);

        builder.ToTable(TableNames.PartyRoles, Schemas.Service);
    }
}
