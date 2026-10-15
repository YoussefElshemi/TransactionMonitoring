using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BackTests;

namespace Presentation.Validators.BackTests;

public class BackTestRequestValidator : AbstractValidator<BackTestRequestDto>
{
    public BackTestRequestValidator(IBackTestService backTestService)
    {
        RuleFor(x => x.BackTestId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await backTestService.ExistsAsync(new OrganizationId(x.OrganizationId), new BackTestId(x.BackTestId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("BackTest not found");
    }
}
