using Core.Enums;
using Core.Models.BackTests;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateBackTestMapper
{
    public static BackTest ToModel(this CreateBackTestRequest request, RuleVersionId ruleVersionId, TimeProvider timeProvider, Mode mode)
    {
        var now = timeProvider.GetUtcNow();

        return new BackTest
        {
            Mode = mode,
            BackTestId = new BackTestId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            RuleId = request.RuleId,
            RuleVersionId = ruleVersionId,
            Status = BackTestStatus.Pending,
            SampleSize = request.SampleSize,
            TotalEvaluations = new TotalCount(0),
            ProcessedCount = new ProcessedCount(0),
            CreatedByUserId = request.CreatedByUserId,
            CompletedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
