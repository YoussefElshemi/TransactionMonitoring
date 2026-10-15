using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.PartyMappings;

namespace Presentation.Validators.PartyMappings;

public class SetPartyMappingRequestValidator : AbstractValidator<SetPartyMappingRequestDto>
{
    public SetPartyMappingRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Mappings)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A party mapping must declare at least one role.")
            .Must(x => x.Select(mapping => mapping.Role).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count)
            .WithMessage("A role must not be mapped twice.")
            .DependentRules(() => { RuleForEach(x => x.Body.Mappings).SetValidator(new PartyRoleMappingValidator()); });
    }
}
