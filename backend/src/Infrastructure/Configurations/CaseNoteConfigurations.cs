using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NoteConfigurations : IEntityTypeConfiguration<CaseNoteEntity>
{
    public void Configure(EntityTypeBuilder<CaseNoteEntity> builder)
    {
        builder.HasKey(x => x.CaseNoteId);

        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.CaseNotes).HasForeignKey(x => x.CreatedByUserId);
        builder.HasOne(x => x.Organization).WithMany(x => x.CaseNotes).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Case).WithMany(x => x.CaseNotes).HasForeignKey(x => x.CaseId).IsRequired();


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CaseNotes, Schemas.Service);
    }
}