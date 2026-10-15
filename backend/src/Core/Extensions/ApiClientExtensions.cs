using System.Net;
using Core.Constants;
using Core.Models.ApiClients;
using FluentValidation;
using FluentValidation.Results;

namespace Core.Extensions;

public static class ApiClientExtensions
{
    public static void ThrowIfNotEnabled(this ApiClient apiClient)
    {
        if (apiClient.OrganizationId == SystemConstants.DefaultOrganization)
        {
            throw Forbidden("You must belong to an organization");
        }

        if (!apiClient.IsEnabled)
        {
            throw Forbidden("This API client is disabled");
        }
    }

    public static void ThrowIfOrganizationNotEnabled(this ApiClient apiClient)
    {
        if (apiClient.Organization is null)
        {
            throw Forbidden("You must belong to an organization");
        }

        if (!apiClient.Organization.Status.TryValidate(out var validationResult))
        {
            throw Forbidden(validationResult);
        }
    }

    private static ValidationException Forbidden(string errorMessage)
    {
        return new ValidationException([
            new ValidationFailure
            {
                ErrorMessage = errorMessage,
                ErrorCode = HttpStatusCode.Forbidden.ToString()
            }
        ]);
    }
}
