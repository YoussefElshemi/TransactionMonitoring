using Core.Enums;
using FluentValidation;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Validators.TransactionSchemas;

public class SchemaFieldValidator : AbstractValidator<SchemaFieldDto>
{
    internal const int MaxAllowedValues = 1000;
    internal const int MaxAllowedValueLength = 256;

    private static readonly SchemaFieldType[] StructuredTypes =
    [
        SchemaFieldType.Object,
        SchemaFieldType.Array
    ];

    public SchemaFieldValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Field name must be provided.")
            .MaximumLength(128)
            .WithMessage("Field name must not be longer than 128 characters.");

        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Field type must be provided.")
            .IsEnumName(typeof(SchemaFieldType), false);

        RuleFor(x => x.ElementType)
            .IsEnumName(typeof(SchemaFieldType), false)
            .When(x => !string.IsNullOrWhiteSpace(x.ElementType));

        RuleFor(x => x.AllowedValues)
            .Cascade(CascadeMode.Stop)
            .Must(x => x!.Count <= MaxAllowedValues)
            .WithMessage($"A field must not allow more than {MaxAllowedValues} values.")
            .Must(x => x!.All(value => !string.IsNullOrWhiteSpace(value)))
            .WithMessage("Allowed values must not be blank.")
            .Must(x => x!.All(value => value.Length <= MaxAllowedValueLength))
            .WithMessage($"An allowed value must not be longer than {MaxAllowedValueLength} characters.")
            .Must(x => x!.Distinct(StringComparer.Ordinal).Count() == x!.Count)
            .WithMessage("Allowed values must not repeat.")
            .When(x => x.AllowedValues is { Count: > 0 });

        RuleFor(x => x)
            .Must(BeStringWhenEnumerated)
            .WithMessage("Allowed values may only be set on a String field.");

        RuleFor(x => x)
            .Custom(ValidateShape);

        When(x => x.Fields is { Count: > 0 }, () =>
        {
            RuleFor(x => x.Fields)
                .Must(x => x!.Where(field => field.Name != null).Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() ==
                           x!.Count(field => field.Name != null))
                .WithMessage("Field names must be unique.")
                .DependentRules(() => { RuleForEach(x => x.Fields).SetValidator(this); });
        });
    }

    private static bool BeStringWhenEnumerated(SchemaFieldDto field)
    {
        if (field.AllowedValues is not { Count: > 0 })
        {
            return true;
        }

        return Enum.TryParse<SchemaFieldType>(field.Type, true, out var type) && type == SchemaFieldType.String;
    }

    private static void ValidateShape(SchemaFieldDto field, ValidationContext<SchemaFieldDto> context)
    {
        if (!Enum.TryParse<SchemaFieldType>(field.Type, true, out var type))
        {
            return;
        }

        var elementType = ParseElementType(field);

        if (type == SchemaFieldType.Array)
        {
            if (elementType == null)
            {
                context.AddFailure("An Array field must declare an ElementType.");

                return;
            }

            if (elementType == SchemaFieldType.Array)
            {
                context.AddFailure("An Array field must not carry arrays.");

                return;
            }
        }
        else if (elementType != null)
        {
            context.AddFailure("ElementType may only be set on an Array field.");
        }

        var declaresMembers = type == SchemaFieldType.Object || (type == SchemaFieldType.Array && elementType == SchemaFieldType.Object);

        switch (declaresMembers)
        {
            case true when field.Fields is not { Count: > 0 }:
                context.AddFailure($"{(type == SchemaFieldType.Object ? "An Object field" : "An Array of objects")} must declare the fields it carries.");
                return;
            case false when field.Fields is { Count: > 0 }:
                context.AddFailure($"Fields may only be declared on an Object field or an Array of objects, not on {Describe(type)}.");
                return;
        }

        if (StructuredTypes.Contains(type) && field.AllowedValues is { Count: > 0 })
        {
            context.AddFailure("Allowed values may only be set on a String field.");
        }
    }

    private static SchemaFieldType? ParseElementType(SchemaFieldDto field)
    {
        return !string.IsNullOrWhiteSpace(field.ElementType) && Enum.TryParse<SchemaFieldType>(field.ElementType, true, out var elementType)
            ? elementType
            : null;
    }

    private static string Describe(SchemaFieldType type)
    {
        return type switch
        {
            SchemaFieldType.String => "a String field",
            SchemaFieldType.Number => "a Number field",
            SchemaFieldType.Boolean => "a Boolean field",
            SchemaFieldType.DateTime => "a DateTime field",
            SchemaFieldType.Array => "an Array of values",
            _ => "an Object field"
        };
    }
}
