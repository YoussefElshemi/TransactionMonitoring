using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Commitments;

namespace Presentation.Validators.Commitments;

public class DeleteCommitmentRequestValidator : AbstractValidator<DeleteCommitmentRequestDto>
{
    public DeleteCommitmentRequestValidator(
        IOrganizationService organizationService,
        ICommitmentService commitmentService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.CommitmentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await commitmentService.ExistsAsync(new OrganizationId(dto.OrganizationId), new CommitmentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Commitment not found");
    }
}
