using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record CustomerMarginLine
{
    public required BillableResource BillableResource { get; init; }
    public required Count Count { get; init; }

    public required UnitCost? UnitCost { get; init; }

    public required CostAmount Cost { get; init; }
}
