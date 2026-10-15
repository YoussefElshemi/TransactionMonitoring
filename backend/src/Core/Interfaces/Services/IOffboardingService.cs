using Core.Models.Users;

namespace Core.Interfaces.Services;

public interface IOffboardingService
{
    Task<OffboardedUser> OffboardAsync(OffboardUserRequest offboardUserRequest, CancellationToken stoppingToken);
}
