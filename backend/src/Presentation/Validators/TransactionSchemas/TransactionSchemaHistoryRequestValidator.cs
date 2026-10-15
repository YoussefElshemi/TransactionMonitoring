using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Validators.TransactionSchemas;

public class TransactionSchemaHistoryRequestValidator : AbstractValidator<TransactionSchemaHistoryRequestDto>
{
    public TransactionSchemaHistoryRequestValidator(IOrganizationService organizationService, ITransactionSchemaService transactionSchemaService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await transactionSchemaService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Transaction schema not found");
    }
}
