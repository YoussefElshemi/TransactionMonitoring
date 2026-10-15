using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class MaintenanceWindowStartedException(MaintenanceWindowId maintenanceWindowId)
    : DomainException($"Maintenance window '{maintenanceWindowId}' has already started. Only a scheduled window can be changed.")
{
    public MaintenanceWindowId MaintenanceWindowId { get; } = maintenanceWindowId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Maintenance window already started";
    }
}
