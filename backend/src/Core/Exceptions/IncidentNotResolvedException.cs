using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class IncidentNotResolvedException(IncidentId incidentId)
    : DomainException($"Incident '{incidentId}' is not resolved. A post-incident review can only be published once the incident is over.")
{
    public IncidentId IncidentId { get; } = incidentId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Incident not resolved";
    }
}
