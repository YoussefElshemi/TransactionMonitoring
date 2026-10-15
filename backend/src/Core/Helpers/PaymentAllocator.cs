using Core.ValueObjects;

namespace Core.Helpers;

public static class PaymentAllocator
{
    public static decimal Allocatable(BalanceAmount balance, OutstandingAmount outstanding)
    {
        return Math.Max(Math.Min(balance, outstanding), 0);
    }

    public static decimal Allocatable(BalanceAmount balance, OutstandingAmount outstanding, decimal requested)
    {
        return Math.Max(Math.Min(Allocatable(balance, outstanding), requested), 0);
    }
}
