namespace Core.Enums;

// Persisted as a foreign key from Payments: never renumber or reuse a member.
public enum PaymentStatus
{
    Pending = 1,
    Settled = 2,
    Failed = 3,
    Returned = 4
}
