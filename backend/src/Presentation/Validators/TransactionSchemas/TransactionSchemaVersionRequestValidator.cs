using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Validators.TransactionSchemas;

public class TransactionSchemaVersionRequestValidator : AbstractValidator<TransactionSchemaVersionRequestDto>
{
    public TransactionSchemaVersionRequestValidator(IOrganizationService organizationService, ITransactionSchemaService transactionSchemaService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.VersionNumber)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(async (x, _, ct) => await transactionSchemaService.VersionExistsAsync(new OrganizationId(x.OrganizationId), new VersionNumber(x.VersionNumber), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Transaction schema version not found");
    }
}
