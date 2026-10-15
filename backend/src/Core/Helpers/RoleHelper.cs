using System.Globalization;
using Core.Enums;

namespace Core.Helpers;

// Built-in role Ids end in the enum value in their last twelve digits.
public static class RoleHelper
{
    private const int SuffixLength = 12;

    public static UserRole MapToUserRole(Guid roleId)
    {
        if (!TryMapToUserRole(roleId, out var userRole))
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), roleId, "The role id does not name a built-in role.");
        }

        return userRole;
    }

    public static bool TryMapToUserRole(Guid roleId, out UserRole userRole)
    {
        var value = roleId.ToString();
        var prefix = value[..^SuffixLength];

        // The prefix must match the seed's zeros, or a random Guid ending in ...000000000002 would read as Manager.
        if (prefix != "00000000-0000-0000-0000-" ||
            !int.TryParse(value[^SuffixLength..], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            !Enum.IsDefined(typeof(UserRole), parsed))
        {
            userRole = default;
            return false;
        }

        userRole = (UserRole)parsed;
        return true;
    }

    public static bool TryParse(string? value, out UserRole userRole)
    {
        // TryParse also accepts numeric strings, so IsDefined is needed to reject undefined values.
        return Enum.TryParse(value, true, out userRole) && Enum.IsDefined(userRole);
    }

    public static UserRole Parse(string? value)
    {
        if (!TryParse(value, out var userRole))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The value does not name a built-in role.");
        }

        return userRole;
    }

    public static Guid MapFromUserRole(UserRole userRole)
    {
        return new Guid("00000000-0000-0000-0000-" + ((int)userRole).ToString("D12"));
    }

    public static bool IsBuiltIn(Guid roleId)
    {
        return TryMapToUserRole(roleId, out _);
    }
}
