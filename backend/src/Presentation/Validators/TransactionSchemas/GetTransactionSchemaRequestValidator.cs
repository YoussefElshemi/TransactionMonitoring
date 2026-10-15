using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Validators.TransactionSchemas;

public class GetTransactionSchemaRequestValidator : AbstractValidator<GetTransactionSchemaRequestDto>
{
    public GetTransactionSchemaRequestValidator(IOrganizationService organizationService, ITransactionSchemaService transactionSchemaService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x)
            .MustAsync(async (x, _, ct) => await transactionSchemaService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(x.IncludeDeleted), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Transaction schema not found")
            .When(x => x.OrganizationId != Guid.Empty);
    }
}
