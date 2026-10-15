using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record TransactionParty : BaseModel
{
    public required TransactionId TransactionId { get; init; }
    public required PartyId PartyId { get; init; }
    public required PartyRole Role { get; init; }
    public required Party? Party { get; init; }
}
