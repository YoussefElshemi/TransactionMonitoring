using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class TransactionPartyMapper
{
    public static TransactionParty ToModel(this TransactionPartyEntity transactionPartyEntity)
    {
        return new TransactionParty
        {
            TransactionId = new TransactionId(transactionPartyEntity.TransactionId),
            PartyId = new PartyId(transactionPartyEntity.PartyId),
            Role = (PartyRole)transactionPartyEntity.PartyRoleId,
            Party = transactionPartyEntity.Party?.ToModel([]),
            CreatedAt = new CreatedAt(transactionPartyEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(transactionPartyEntity.UpdatedAt)
        };
    }

    public static TransactionPartyEntity ToEntity(this TransactionParty transactionParty)
    {
        return new TransactionPartyEntity
        {
            TransactionId = transactionParty.TransactionId,
            PartyId = transactionParty.PartyId,
            PartyRoleId = (int)transactionParty.Role,
            CreatedAt = transactionParty.CreatedAt,
            UpdatedAt = transactionParty.UpdatedAt
        };
    }
}
