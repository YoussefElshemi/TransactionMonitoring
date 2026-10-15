using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Usages;

namespace Presentation.Validators.Usages;

public class UsageRequestValidator : AbstractValidator<UsageRequestDto>
{
    public UsageRequestValidator(IUsageService usageService)
    {
        RuleFor(x => x.UsageId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await usageService.ExistsAsync(new OrganizationId(x.OrganizationId), new UsageId(x.UsageId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Usage not found");
    }
}