using System.Security.Claims;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.Users;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace Core.Services;

public class ApplicationUserService(IApplicationUserRepository applicationUserRepository) : IApplicationUserService
{
    public Task<ApplicationUser> GetAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken)
    {
        return applicationUserRepository.GetAsync(claimsPrincipal, stoppingToken);
    }

    public Task<ApplicationUser> GetAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        return applicationUserRepository.GetByIdAsync(userId, stoppingToken);
    }

    public Task<ApplicationUser?> FindByEmailAsync(Email email, CancellationToken stoppingToken)
    {
        return applicationUserRepository.FindByEmailAsync(email, stoppingToken);
    }

    public async Task CreateAsync(CreateApplicationUserRequest createApplicationUserRequest, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.CreateAsync(createApplicationUserRequest, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public Task<List<ApplicationUser>> ListByIdsAsync(IReadOnlyCollection<ApplicationUserId> userIds, CancellationToken stoppingToken)
    {
        return applicationUserRepository.ListByIdsAsync(userIds, stoppingToken);
    }

    public async Task SetSubOrganizationAsync(ApplicationUserId userId, SubOrganizationId? subOrganizationId, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.SetSubOrganizationAsync(userId, subOrganizationId, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public async Task SetDisplayPreferencesAsync(
        ApplicationUserId userId, Patch<Locale?> locale, Patch<DisplayTimeZone?> timeZone, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.SetDisplayPreferencesAsync(userId, locale, timeZone, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public Task AddRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken)
    {
        return applicationUserRepository.AddRoleAsync(userId, userRole, stoppingToken);
    }

    public Task RemoveRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken)
    {
        return applicationUserRepository.RemoveRoleAsync(userId, userRole, stoppingToken);
    }

    public Task<PagedResponse<ApplicationUser>> ListAsync(ListUserRequest listUserRequest, CancellationToken stoppingToken)
    {
        return applicationUserRepository.ListAsync(listUserRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return applicationUserRepository.ExistsByIdAsync(userId, includeDeleted, stoppingToken);
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.UpdateAsync(user, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public async Task DeleteAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.DeleteAsync(userId, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public async Task RecoverAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var result = await applicationUserRepository.RecoverAsync(userId, stoppingToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => new ValidationFailure
            {
                ErrorMessage = x.Description,
                ErrorCode = x.Code
            }));
        }
    }

    public Task<TotalCount> GetCountAsync(ListUserRequest listUserRequest, CancellationToken stoppingToken)
    {
        return applicationUserRepository.GetCountAsync(listUserRequest, stoppingToken);
    }
}