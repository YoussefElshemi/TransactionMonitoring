using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Commitments;

namespace Presentation.Validators.Commitments;

public class CreateCommitmentRequestValidator : AbstractValidator<CreateCommitmentRequestDto>
{
    public CreateCommitmentRequestValidator(
        IOrganizationService organizationService,
        TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Amount)
            .GreaterThan(0)
            .WithMessage("A commitment of nothing is not a commitment");

        RuleFor(x => x.Body.EffectiveTo)
            .GreaterThan(x => x.Body.EffectiveFrom ?? timeProvider.GetUtcNow())
            .WithMessage("EffectiveTo must be after EffectiveFrom");
    }
}
