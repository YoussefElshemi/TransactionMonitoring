using Core.Helpers;
using Core.Models.BackTests;
using Presentation.Models.BackTests;

namespace Presentation.Mappers.BackTests;

public static class BackTestResultDtoMapper
{
    public static BackTestResultResponseDto ToDto(this BackTestResult result)
    {
        return new BackTestResultResponseDto
        {
            BackTestResultId = result.BackTestResultId,
            RuleEvaluationId = result.RuleEvaluationId,
            Payload = PayloadMasker.Mask(result.Payload),
            PayloadMasked = true,
            OriginalMatched = result.OriginalMatched,
            NewMatched = result.NewMatched,
            HasChanged = result.HasChanged,
            CreatedAt = result.CreatedAt
        };
    }
}
