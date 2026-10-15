using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Statistics;

public record StatisticRequest : DatedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}