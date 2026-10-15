using Core.Interfaces;

namespace Infrastructure;

public class AuditContext : IAuditContext
{
    public Guid? ActingUserId { get; set; }
    public Guid? ActingApiClientId { get; set; }
}
