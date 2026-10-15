using Core.Enums;

namespace Core.Extensions;

public static class OrganizationStatusExtensions
{
    public static bool TryValidate(this OrganizationStatus organizationStatus, out string validationResult)
    {
        switch (organizationStatus)
        {
            case OrganizationStatus.Enabled:
                validationResult = null!;
                return true;

            case OrganizationStatus.OverdueInvoice:
                validationResult = "Organization must pay overdue invoice";
                return false;

            case OrganizationStatus.Disabled:
                validationResult = "Organization must be enabled";
                return false;

            default:
                throw new ArgumentOutOfRangeException(nameof(organizationStatus), organizationStatus, null);
        }
    }
}