using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class QueueMemberConfigurations : IEntityTypeConfiguration<QueueMemberEntity>
{
    public void Configure(EntityTypeBuilder<QueueMemberEntity> builder)
    {
        builder.HasKey(x => new { x.QueueId, x.UserId });

        builder.HasOne(x => x.User).WithMany(x => x.QueueMemberships).HasForeignKey(x => x.UserId).IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.QueueMembers, Schemas.Service);
    }
}
