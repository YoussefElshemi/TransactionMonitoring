using Core.Enums;
using Core.Helpers;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.PartyMappings;

namespace Presentation.Validators.PartyMappings;

public class PartyRoleMappingValidator : AbstractValidator<PartyRoleMappingDto>
{
    internal const int MaxAttributes = 20;

    public PartyRoleMappingValidator()
    {
        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A mapping must name the role it resolves.")
            .IsEnumName(typeof(PartyRole), false);

        RuleFor(x => x.Path)
            .Must(PayloadPath.IsAddressable!)
            .WithMessage("Path is not a path the engine can read.")
            .When(x => !string.IsNullOrWhiteSpace(x.Path));

        RuleFor(x => x.IdentifierPath)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A mapping must say which field identifies the party.")
            .Must(PayloadPath.IsAddressable)
            .WithMessage("IdentifierPath is not a path the engine can read.")
            .Must(x => !PayloadPath.HasWildcard(x))
            .WithMessage("IdentifierPath must not carry a wildcard — put it on Path instead.");

        RuleFor(x => x.DisplayNamePath)
            .Cascade(CascadeMode.Stop)
            .Must(PayloadPath.IsAddressable!)
            .WithMessage("DisplayNamePath is not a path the engine can read.")
            .Must(x => !PayloadPath.HasWildcard(x!))
            .WithMessage("DisplayNamePath must not carry a wildcard — put it on Path instead.")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayNamePath));

        When(x => x.AttributePaths is { Count: > 0 }, () =>
        {
            RuleFor(x => x.AttributePaths)
                .Cascade(CascadeMode.Stop)
                .Must(x => x!.Count <= MaxAttributes)
                .WithMessage($"A role must not carry more than {MaxAttributes} attributes.")
                .Must(x => x!.Select(attribute => attribute.Name).Distinct(StringComparer.Ordinal).Count() == x!.Count)
                .WithMessage("Attribute names must be unique.")
                .DependentRules(() => { RuleForEach(x => x.AttributePaths).SetValidator(new PartyAttributeMappingValidator()); });
        });
    }
}

public class PartyAttributeMappingValidator : AbstractValidator<PartyAttributeMappingDto>
{
    public PartyAttributeMappingValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("An attribute must be named.")
            .MaximumLength(PartyAttributeName.MaxLength)
            .WithMessage($"An attribute name must not be longer than {PartyAttributeName.MaxLength} characters.");

        RuleFor(x => x.Path)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("An attribute must say where to read it from.")
            .Must(PayloadPath.IsAddressable)
            .WithMessage("An attribute path is not a path the engine can read.")
            .Must(x => !PayloadPath.HasWildcard(x))
            .WithMessage("An attribute path must not carry a wildcard — put it on Path instead.");
    }
}
