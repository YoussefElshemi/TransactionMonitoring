using System.Text.Json.Nodes;
using Core.Models.BackTests;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class BackTestResultMapper
{
    public static BackTestResult ToModel(this BackTestResultEntity entity, JsonObject payload)
    {
        return new BackTestResult
        {
            BackTestResultId = new BackTestResultId(entity.BackTestResultId),
            BackTestId = new BackTestId(entity.BackTestId),
            RuleEvaluationId = new RuleEvaluationId(entity.RuleEvaluationId),
            Payload = payload,
            OriginalMatched = new Matched(entity.OriginalMatched),
            NewMatched = new Matched(entity.NewMatched),
            HasChanged = new HasChanged(entity.HasChanged),
            CreatedAt = new CreatedAt(entity.CreatedAt),
            UpdatedAt = new UpdatedAt(entity.UpdatedAt)
        };
    }

    public static BackTestResultEntity ToEntity(this BackTestResult model)
    {
        return new BackTestResultEntity
        {
            BackTestResultId = model.BackTestResultId,
            BackTestId = model.BackTestId,
            RuleEvaluationId = model.RuleEvaluationId,
            OriginalMatched = model.OriginalMatched,
            NewMatched = model.NewMatched,
            HasChanged = model.HasChanged,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}
