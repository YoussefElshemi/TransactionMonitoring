using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class CommitmentInUseException(CommitmentId commitmentId)
    : DomainException($"Commitment '{commitmentId}' has already been drawn against by an issued invoice and cannot be deleted.")
{
    public CommitmentId CommitmentId { get; } = commitmentId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Commitment is in use";
    }
}
