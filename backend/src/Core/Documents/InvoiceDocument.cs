using System.Globalization;
using Core.Configs;
using Core.Constants;
using Core.Documents.Components;
using Core.Enums;
using Core.Helpers;
using Core.Localization;
using Core.Models.Invoices;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Core.Documents;

public class InvoiceDocument(
    Invoice invoice,
    InvoiceConfig invoiceConfig,
    Translator translator
    ) : IDocument
{
    private const float TotalsWidth = 230;

    private bool IsIssued => invoice.InvoiceStatus is InvoiceStatus.Issued;

    private string SupplierName => invoice.Supplier?.LegalName.ToString() ?? invoiceConfig.CompanyName;

    private string Reference => IsIssued
        ? invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString()
        : translator.Text(TranslationKeys.DocumentInvoiceDraftTitle);

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.MarginHorizontal(48);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(DocumentStyle.Body).LineHeight(1.3f));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(22).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(title =>
                {
                    if (IsIssued)
                    {
                        title.Item().Text(Label(TranslationKeys.DocumentInvoiceTitle))
                            .FontSize(10).SemiBold().FontColor(DocumentStyle.Accent);
                    }

                    title.Item().Text(Reference)
                        .FontSize(22).Bold().LineHeight(1.15f)
                        .FontColor(IsIssued ? DocumentStyle.Ink : DocumentStyle.Draft);
                });

                row.ConstantItem(220).AlignRight().AlignBottom().Text(SupplierName)
                    .FontSize(11).SemiBold().FontColor(DocumentStyle.Ink);
            });

            column.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(DocumentStyle.Ink);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(26);

            column.Item().Element(ComposeFacts);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new PartyComponent(
                    translator.Text(TranslationKeys.DocumentFrom), invoice.Supplier, invoiceConfig.CompanyName, translator));
                row.ConstantItem(40);
                row.RelativeItem().Component(new PartyComponent(
                    translator.Text(TranslationKeys.DocumentFor), invoice.Customer, invoice.Organization?.FriendlyName ?? string.Empty, translator));
            });

            column.Item().Element(ComposeTable);

            column.Item().ShowEntire().Row(row =>
            {
                row.RelativeItem().Element(ComposeNotes);
                row.ConstantItem(40);
                row.ConstantItem(TotalsWidth).Element(ComposeTotals);
            });
        });
    }

    private void ComposeFacts(IContainer container)
    {
        container.Background(DocumentStyle.Panel).PaddingVertical(12).PaddingHorizontal(16).Row(row =>
        {
            row.Spacing(12);

            row.RelativeItem().Element(x => ComposeFact(x,
                Label(TranslationKeys.DocumentBillingPeriod),
                $"{invoice.Month}/{invoice.Year}"));

            row.RelativeItem().Element(x => ComposeFact(x,
                Label(TranslationKeys.DocumentIssueDate),
                invoice.IssuedAt.HasValue
                    ? invoice.IssuedAt.Value.ToString(DateTimeConstants.DateFormat)
                    : translator.Text(TranslationKeys.DocumentNotYetIssued)));

            row.RelativeItem().Element(x => ComposeFact(x,
                Label(TranslationKeys.DocumentDueDate),
                invoice.DueAt.HasValue
                    ? invoice.DueAt.Value.ToString(DateTimeConstants.DateFormat)
                    : translator.Text(TranslationKeys.DocumentDueInDays, (int)invoice.PaymentTerms)));

            row.RelativeItem().Element(x => ComposeFact(x,
                Label(TranslationKeys.DocumentCurrency),
                CurrencyFormatter.Code(invoice.Currency)));
        });
    }

    private static void ComposeFact(IContainer container, string label, string value)
    {
        container.Column(column =>
        {
            column.Spacing(2);

            column.Item().Text(label).FontSize(DocumentStyle.LabelSize).FontColor(DocumentStyle.Muted);
            column.Item().Text(value).FontSize(10.5f).SemiBold().FontColor(DocumentStyle.Ink);
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(70);
                columns.ConstantColumn(75);
                columns.ConstantColumn(90);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text(translator.Text(TranslationKeys.DocumentColumnDescription));
                header.Cell().Element(HeaderCellStyle).AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnQuantity));
                header.Cell().Element(HeaderCellStyle).AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnUnitPrice));
                header.Cell().Element(HeaderCellStyle).AlignRight().Text(translator.Text(TranslationKeys.DocumentColumnAmount));
            });

            foreach (var line in invoice.Lines.OrderBy(x => (int)x.SortOrder))
            {
                table.Cell().Element(CellStyle).PaddingRight(12).Text($"{line.Description}").FontColor(DocumentStyle.Ink);

                var metered = line.InvoiceLineType == InvoiceLineType.Usage;

                table.Cell().Element(CellStyle).AlignRight()
                    .Text(metered ? line.Quantity.ToString("N0", CultureInfo.InvariantCulture) : string.Empty);
                table.Cell().Element(CellStyle).AlignRight().Text(metered ? Money(line.UnitAmount, 4) : string.Empty);
                table.Cell().Element(CellStyle).AlignRight().Text(Money(line.Amount)).FontColor(DocumentStyle.Ink);
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(Label(TranslationKeys.DocumentSubtotal)).FontColor(DocumentStyle.Muted);
                row.AutoItem().Text(Money(invoice.NetAmount)).FontColor(DocumentStyle.Ink);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(Label(TranslationKeys.DocumentTax, invoice.TaxRate.ToString("0.##")))
                    .FontColor(DocumentStyle.Muted);
                row.AutoItem().Text(Money(invoice.TaxAmount)).FontColor(DocumentStyle.Ink);
            });

            column.Item().PaddingTop(5).BorderTop(1.5f).BorderColor(DocumentStyle.Ink).PaddingTop(9).Row(row =>
            {
                row.RelativeItem().AlignBottom().Text(Label(TranslationKeys.DocumentTotal))
                    .FontSize(11).SemiBold().FontColor(DocumentStyle.Ink);
                row.AutoItem().Text(Money(invoice.Amount))
                    .FontSize(15).Bold().FontColor(DocumentStyle.Ink);
            });
        });
    }

    private void ComposeNotes(IContainer container)
    {
        container.DefaultTextStyle(x => x.FontSize(DocumentStyle.NoteSize).FontColor(DocumentStyle.Muted)).Column(column =>
        {
            column.Spacing(6);

            if (invoice.ConversionRate.HasValue)
            {
                column.Item().Text(translator.Text(
                    TranslationKeys.DocumentConversionNote,
                    CurrencyFormatter.Code(invoice.PlanCurrency),
                    invoice.ConversionRate.Value.ToString("0.######"),
                    CurrencyFormatter.Code(invoice.Currency)));
            }

            if (invoice.TaxTreatment is not TaxTreatment.Standard)
            {
                column.Item().Text(TaxNote());
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(14).BorderTop(0.75f).BorderColor(DocumentStyle.Hairline).PaddingTop(8)
            .DefaultTextStyle(x => x.FontSize(DocumentStyle.LabelSize).FontColor(DocumentStyle.Muted))
            .Row(row =>
            {
                row.RelativeItem().Text($"{SupplierName}  ·  {Reference}");

                row.AutoItem().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
    }

    private string TaxNote()
    {
        var taxLine = invoice.Lines.Find(x => x.InvoiceLineType == InvoiceLineType.Tax);

        return taxLine is not null ? taxLine.Description : string.Empty;
    }

    private string Money(decimal amount, int decimals = 2)
    {
        return CurrencyFormatter.Format(invoice.Currency, amount, decimals);
    }

    private string Label(string key, params object?[] arguments)
    {
        return translator.Text(key, [.. arguments, string.Empty]).Trim(' ', ':', ' ', ' ');
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .DefaultTextStyle(x => x.FontSize(DocumentStyle.LabelSize).SemiBold().FontColor(DocumentStyle.Muted))
            .BorderBottom(1).BorderColor(DocumentStyle.Ink).PaddingBottom(6);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(0.75f).BorderColor(DocumentStyle.Hairline).PaddingVertical(7);
    }
}
