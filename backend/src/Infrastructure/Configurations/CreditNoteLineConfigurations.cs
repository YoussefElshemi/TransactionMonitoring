using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CreditNoteLineConfigurations : IEntityTypeConfiguration<CreditNoteLineEntity>
{
    public void Configure(EntityTypeBuilder<CreditNoteLineEntity> builder)
    {
        builder.HasKey(x => x.CreditNoteLineId);

        builder.HasOne(x => x.CreditNote).WithMany(x => x.Lines).HasForeignKey(x => x.CreditNoteId).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(InvoiceLineDescription.MaxLength);

        builder.HasIndex(x => new { x.CreditNoteId, x.SortOrder }).IsUnique();

        builder.ToTable(TableNames.CreditNoteLines, Schemas.Service);
    }
}
