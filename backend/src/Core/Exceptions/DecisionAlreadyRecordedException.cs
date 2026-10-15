using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class DecisionAlreadyRecordedException(TransactionId transactionId)
    : DomainException($"A decision has already been recorded for transaction '{transactionId}'.")
{
    public TransactionId TransactionId { get; } = transactionId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Transaction has already been decided";
    }

    public override object GetExtensions()
    {
        return new
        {
            TransactionId = TransactionId.ToString()
        };
    }
}
