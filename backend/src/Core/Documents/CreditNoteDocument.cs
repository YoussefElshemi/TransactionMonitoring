using Core.Configs;
using Core.Constants;
using Core.Documents.Components;
using Core.Enums;
using Core.Helpers;
using Core.Localization;
using Core.Models.CreditNotes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Core.Documents;

public class CreditNoteDocument(
    CreditNote creditNote,
    InvoiceConfig invoiceConfig,
    Translator translator
    ) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column
                    .Item().Text(translator.Text(TranslationKeys.DocumentCreditNoteTitle, creditNote.CreditNoteNumber.ToString()))
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentAgainstInvoice)).SemiBold();
                    text.Span(creditNote.Invoice?.InvoiceNumber?.ToString() ?? creditNote.InvoiceId.ToString());
                });

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentIssueDate)).SemiBold();
                    text.Span(creditNote.IssuedAt.ToString(DateTimeConstants.DateFormat));
                });

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentCurrency)).SemiBold();
                    text.Span(CurrencyFormatter.Code(creditNote.Currency));
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new PartyComponent(
                    translator.Text(TranslationKeys.DocumentFrom), creditNote.Invoice?.Supplier, invoiceConfig.CompanyName, translator));
                row.ConstantItem(50);
                row.RelativeItem().Component(new PartyComponent(
                    translator.Text(TranslationKeys.DocumentFor), creditNote.Invoice?.Customer,
                    creditNote.Invoice?.Organization?.FriendlyName ?? string.Empty, translator));
            });

            column.Item().Text(text =>
            {
                text.Span(translator.Text(TranslationKeys.DocumentReason)).SemiBold();
                text.Span(creditNote.Reason.ToString());
            });

            column.Item().Element(ComposeTable);
            column.Item().Element(ComposeTotals);

            if (creditNote.TaxTreatment is not TaxTreatment.Standard)
            {
                column.Item().Text(translator.Text(
                        TranslationKeys.DocumentCreditTermsNote, translator.TaxTreatment(creditNote.TaxTreatment)))
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private void ComposeTable(IContainer container)
    {
        var headerStyle = TextStyle.Default.SemiBold();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Text(translator.Text(TranslationKeys.DocumentColumnDescription)).Style(headerStyle);
                header.Cell().AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnQuantity)).Style(headerStyle);
                header.Cell().AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnUnitPrice)).Style(headerStyle);
                header.Cell().AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnAmount)).Style(headerStyle);

                header.Cell().ColumnSpan(4).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
            });

            foreach (var line in creditNote.Lines.OrderBy(x => (int)x.SortOrder))
            {
                table.Cell().Element(CellStyle).Text($"{line.Description}");
                table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity > 1 ? $"{line.Quantity}" : string.Empty);
                table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity > 1 ? Money(line.UnitAmount, 4) : string.Empty);
                table.Cell().Element(CellStyle).AlignRight().Text(Money(line.Amount));
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Column(column =>
        {
            column.Item().PaddingRight(5).Text(
                translator.Text(TranslationKeys.DocumentSubtotalCredited, Money(creditNote.NetAmount)));

            column.Item().PaddingRight(5).Text(translator.Text(
                TranslationKeys.DocumentTax, creditNote.TaxRate.ToString("0.##"), Money(creditNote.TaxAmount)));

            column.Item().PaddingRight(5).PaddingTop(3)
                .Text(translator.Text(TranslationKeys.DocumentTotalCredited, Money(creditNote.Amount))).SemiBold();
        });
    }

    private string Money(decimal amount, int decimals = 2)
    {
        return CurrencyFormatter.Format(creditNote.Currency, amount, decimals);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }
}
