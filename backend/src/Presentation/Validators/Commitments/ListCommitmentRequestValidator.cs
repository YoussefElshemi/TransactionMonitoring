using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Commitments;
using Presentation.Models.Commitments;
using Presentation.Validators.Common;

namespace Presentation.Validators.Commitments;

public class ListCommitmentRequestValidator(
    IOrganizationService organizationService,
    ICommitmentService commitmentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCommitmentRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListCommitmentRequestDto request, CancellationToken stoppingToken)
    {
        return commitmentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
