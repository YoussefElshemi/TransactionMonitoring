using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.TransactionSchemas;
using Presentation.Models.TransactionSchemas;
using Presentation.Validators.Common;

namespace Presentation.Validators.TransactionSchemas;

public class ListTransactionSchemaVersionRequestValidator(
    ITransactionSchemaService transactionSchemaService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListTransactionSchemaVersionRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListTransactionSchemaVersionRequestDto request, CancellationToken stoppingToken)
    {
        return transactionSchemaService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
