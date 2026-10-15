using Core.Models.Commitments;
using Core.ValueObjects;
using Presentation.Models.Commitments;

namespace Presentation.Mappers.Commitments;

public static class CommitmentMapper
{
    public static CreateCommitmentRequest ToModel(this CreateCommitmentRequestDto dto)
    {
        return new CreateCommitmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Amount = new CommitmentAmount(dto.Body.Amount),
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            EffectiveTo = new EffectiveTo(dto.Body.EffectiveTo),
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListCommitmentRequest ToModel(this ListCommitmentRequestDto dto)
    {
        return new ListCommitmentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeExpired = new IncludeExpired(dto.IncludeExpired ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static CommitmentResponseDto ToDto(this Commitment commitment)
    {
        return new CommitmentResponseDto
        {
            CommitmentId = commitment.CommitmentId,
            OrganizationId = commitment.OrganizationId,
            Amount = commitment.Amount,
            EffectiveFrom = commitment.EffectiveFrom,
            EffectiveTo = commitment.EffectiveTo,
            Drawn = commitment.Drawn,
            Remaining = commitment.Amount - commitment.Drawn,
            CreatedByUserId = commitment.CreatedByUserId.HasValue ? commitment.CreatedByUserId.Value : null,
            CreatedAt = commitment.CreatedAt,
            UpdatedAt = commitment.UpdatedAt
        };
    }
}
