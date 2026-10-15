using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.ServiceCredits;

namespace Presentation.Validators.ServiceCredits;

public class IssueServiceCreditRequestValidator : AbstractValidator<IssueServiceCreditRequestDto>
{
    public IssueServiceCreditRequestValidator(IOrganizationService organizationService, TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Month)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.Body.Year)
            .InclusiveBetween(2000, 2100);

        RuleFor(x => x)
            .Must(x =>
            {
                var now = timeProvider.GetUtcNow();
                return new DateTimeOffset(x.Body.Year, x.Body.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1) <= now;
            })
            .When(x => x.Body.Month is >= 1 and <= 12 && x.Body.Year is >= 2000 and <= 2100)
            .WithMessage("A month can only be measured once it has ended");
    }
}
