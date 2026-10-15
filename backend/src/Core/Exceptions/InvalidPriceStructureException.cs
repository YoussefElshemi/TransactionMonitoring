using System.Net;

namespace Core.Exceptions;

public class InvalidPriceStructureException(string reason)
    : DomainException($"The pricing on this plan version is not usable: {reason}")
{
    public string Reason { get; } = reason;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Invalid price structure";
    }
}
