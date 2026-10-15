namespace Core.Interfaces;

public interface IAuditContext
{
    Guid? ActingUserId { get; set; }
    Guid? ActingApiClientId { get; set; }
}
