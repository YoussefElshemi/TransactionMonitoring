using System.Net;
using Core.Models.Users;
using FluentValidation;
using FluentValidation.Results;

namespace Core.Extensions;

public static class ApplicationUserExtensions
{
    public static void ThrowIfNotEnabled(this ApplicationUser applicationUser)
    {
        if (applicationUser is not { IsEnabled: true })
        {
            throw new ValidationException([
                new ValidationFailure
                {
                    ErrorMessage = "You must belong to an organization",
                    ErrorCode = HttpStatusCode.Forbidden.ToString()
                }
            ]);
        }
    }

    public static void ThrowIfOrganizationNotEnabled(this ApplicationUser applicationUser)
    {
        if (!applicationUser.Organization.Status.TryValidate(out var validationResult))
        {
            throw new ValidationException([
                new ValidationFailure
                {
                    ErrorMessage = validationResult,
                    ErrorCode = HttpStatusCode.Forbidden.ToString()
                }
            ]);
        }
    }
}