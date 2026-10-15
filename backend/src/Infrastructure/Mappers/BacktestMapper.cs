using Core.Enums;
using Core.Models.BackTests;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class BackTestMapper
{
    public static BackTest ToModel(this BackTestEntity entity)
    {
        return new BackTest
        {
            Mode = (Mode)entity.ModeId,
            BackTestId = new BackTestId(entity.BackTestId),
            OrganizationId = new OrganizationId(entity.OrganizationId),
            RuleId = new RuleId(entity.RuleId),
            RuleVersionId = new RuleVersionId(entity.RuleVersionId),
            Status = (BackTestStatus)entity.StatusId,
            SampleSize = new SampleSize(entity.SampleSize),
            TotalEvaluations = new TotalCount(entity.TotalEvaluations),
            ProcessedCount = new ProcessedCount(entity.ProcessedCount),
            CreatedByUserId = new ApplicationUserId(entity.CreatedByUserId),
            CompletedAt = entity.CompletedAt.HasValue ? new CompletedAt(entity.CompletedAt.Value) : null,
            CreatedAt = new CreatedAt(entity.CreatedAt),
            UpdatedAt = new UpdatedAt(entity.UpdatedAt)
        };
    }

    public static BackTestEntity ToEntity(this BackTest model)
    {
        return new BackTestEntity
        {
            ModeId = (int)model.Mode,
            BackTestId = model.BackTestId,
            OrganizationId = model.OrganizationId,
            RuleId = model.RuleId,
            RuleVersionId = model.RuleVersionId,
            StatusId = (int)model.Status,
            SampleSize = model.SampleSize,
            TotalEvaluations = model.TotalEvaluations,
            ProcessedCount = model.ProcessedCount,
            CreatedByUserId = model.CreatedByUserId,
            CompletedAt = model.CompletedAt,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}
