using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class IncidentResolvedException(IncidentId incidentId)
    : DomainException($"Incident '{incidentId}' is resolved. Open a new incident rather than reopening this one.")
{
    public IncidentId IncidentId { get; } = incidentId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Incident already resolved";
    }
}
