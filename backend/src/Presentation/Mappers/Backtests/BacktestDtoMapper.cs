using Core.Models.BackTests;
using Presentation.Models.BackTests;

namespace Presentation.Mappers.BackTests;

public static class BackTestDtoMapper
{
    public static BackTestResponseDto ToDto(this BackTest backTest)
    {
        return new BackTestResponseDto
        {
            Mode = backTest.Mode.ToString(),
            BackTestId = backTest.BackTestId,
            RuleId = backTest.RuleId,
            RuleVersionId = backTest.RuleVersionId,
            Status = backTest.Status.ToString(),
            SampleSize = backTest.SampleSize,
            TotalEvaluations = backTest.TotalEvaluations,
            ProcessedCount = backTest.ProcessedCount,
            CreatedByUserId = backTest.CreatedByUserId,
            CreatedAt = backTest.CreatedAt,
            UpdatedAt = backTest.UpdatedAt,
            CompletedAt = backTest.CompletedAt
        };
    }
}
