using Core.Constants;
using Core.Documents.Components;
using Core.Enums;
using Core.Helpers;
using Core.Localization;
using Core.Models.Billing;
using Core.Models.Invoices;
using Core.Models.Payments;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Core.Documents;

public class ReceiptDocument(
    Payment payment,
    Invoice? invoice,
    BillingEntity billingEntity,
    BillingProfile billingProfile,
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
                    .Item().Text(translator.Text(TranslationKeys.DocumentReceiptTitle, payment.ReceiptNumber?.ToString() ?? string.Empty))
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                if (invoice is not null)
                {
                    column.Item().Text(text =>
                    {
                        text.Span(translator.Text(TranslationKeys.DocumentAgainstInvoice)).SemiBold();
                        text.Span(invoice.InvoiceNumber?.ToString() ?? invoice.InvoiceId.ToString());
                    });
                }

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentReceiptDate)).SemiBold();

                    text.Span(payment.SettledAt!.Value.ToString(DateTimeConstants.DateFormat));
                });

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentPaymentMethod)).SemiBold();
                    text.Span(DescribeMethod());
                });

                column.Item().Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentCurrency)).SemiBold();
                    text.Span(CurrencyFormatter.Code(payment.Currency));
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
                    translator.Text(TranslationKeys.DocumentFrom), invoice?.Supplier, billingEntity.LegalName, translator));
                row.ConstantItem(50);
                row.RelativeItem().Component(new PartyComponent(
                    translator.Text(TranslationKeys.DocumentFor), invoice?.Customer, billingProfile.LegalName, translator));
            });

            column.Item().AlignRight().Text(translator.Text(
                    TranslationKeys.DocumentAmountReceived, CurrencyFormatter.Format(payment.Currency, payment.Amount)))
                .FontSize(14).SemiBold();

            if (invoice is null)
            {
                column.Item().Text(translator.Text(TranslationKeys.DocumentReceiptUnappliedNote))
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            }

            column.Item().Text(translator.Text(TranslationKeys.DocumentReceiptNote))
                .FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private string DescribeMethod()
    {
        var kind = translator.Text(payment.PaymentMethodKind switch
        {
            PaymentMethodKind.DirectDebit => TranslationKeys.PaymentMethodDirectDebit,
            PaymentMethodKind.BankTransfer => TranslationKeys.PaymentMethodBankTransfer,
            _ => TranslationKeys.PaymentMethodCard
        });

        return payment.PaymentMethodLabel is not null ? $"{kind} — {payment.PaymentMethodLabel}" : kind;
    }
}
