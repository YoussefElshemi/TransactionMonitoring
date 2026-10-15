using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Status;

namespace Presentation.Validators.Status;

public class MaintenanceWindowRequestValidator : AbstractValidator<MaintenanceWindowRequestDto>
{
    public MaintenanceWindowRequestValidator(IMaintenanceWindowService maintenanceWindowService)
    {
        RuleFor(x => x.MaintenanceWindowId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await maintenanceWindowService.ExistsAsync(new MaintenanceWindowId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Maintenance window not found");
    }
}
