using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Validators.TransactionSchemas;

public class SetTransactionSchemaRequestValidator : AbstractValidator<SetTransactionSchemaRequestDto>
{
    internal const int MaxFields = 200;
    internal const int MaxNesting = 5;

    public SetTransactionSchemaRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Fields)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A schema must declare at least one field.")
            .Must(x => Count(x) <= MaxFields)
            .WithMessage($"A schema must not declare more than {MaxFields} fields.")
            .Must(x => Nesting(x) <= MaxNesting)
            .WithMessage($"A schema must not nest fields more than {MaxNesting} deep.")
            .Must(x => x.Where(field => field.Name != null).Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() ==
                       x.Count(field => field.Name != null))
            .WithMessage("Field names must be unique.")
            .DependentRules(() => { RuleForEach(x => x.Body.Fields).SetValidator(new SchemaFieldValidator()); });
    }

    private static int Count(List<SchemaFieldDto> fields)
    {
        return fields.Sum(x => 1 + (x.Fields != null ? Count(x.Fields) : 0));
    }

    private static int Nesting(List<SchemaFieldDto> fields)
    {
        return fields.Count == 0 ? 0 : fields.Max(x => 1 + (x.Fields != null ? Nesting(x.Fields) : 0));
    }
}
