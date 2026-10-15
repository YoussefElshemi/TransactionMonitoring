using Core.Localization;
using Core.Models.Billing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Core.Documents.Components;

public class PartyComponent(string title, DocumentParty? party, string fallbackName, Translator translator) : IComponent
{
    public void Compose(IContainer container)
    {
        container.ShowEntire().Column(column =>
        {
            column.Spacing(2);

            column.Item().PaddingBottom(3).Text(title).FontSize(DocumentStyle.LabelSize).FontColor(DocumentStyle.Muted);

            if (party is null)
            {
                column.Item().Text(fallbackName).FontSize(11).SemiBold().FontColor(DocumentStyle.Ink);

                return;
            }

            column.Item().Text(party.LegalName.ToString()).FontSize(11).SemiBold().FontColor(DocumentStyle.Ink);

            foreach (var line in ((string)party.Address).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                column.Item().Text(line);
            }

            column.Item().Text(party.CountryCode.ToString());

            if (party.TaxIdentifier.HasValue)
            {
                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span(translator.Text(TranslationKeys.DocumentTaxIdentifier)).FontColor(DocumentStyle.Muted);
                    text.Span(party.TaxIdentifier.Value.ToString());
                });
            }
        });
    }
}
