using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Parties;

namespace Presentation.Validators.Parties;

public class SetPartyRiskRequestValidator : AbstractValidator<SetPartyRiskRequestDto>
{
    public SetPartyRiskRequestValidator(IPartyService partyService)
    {
        RuleFor(x => x.PartyId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await partyService.ExistsAsync(new OrganizationId(x.OrganizationId), new PartyId(x.PartyId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party not found");

        RuleFor(x => x.Body.Note)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A note must say why the risk score was set.")
            .MaximumLength(RiskNote.MaxLength)
            .WithMessage($"A note must not be longer than {RiskNote.MaxLength} characters.");

        RuleFor(x => x.Body.RiskScore)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Body.RiskScore.HasValue);
    }
}
