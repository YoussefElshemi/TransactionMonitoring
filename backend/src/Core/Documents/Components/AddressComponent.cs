using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Core.Documents.Components;

public class AddressComponent(string title, string companyName) : IComponent
{
    public void Compose(IContainer container)
    {
        container.ShowEntire().Column(column =>
        {
            column.Spacing(2);

            column.Item().Text(title).SemiBold();
            column.Item().PaddingBottom(5).LineHorizontal(1);

            column.Item().Text(companyName);
        });
    }
}