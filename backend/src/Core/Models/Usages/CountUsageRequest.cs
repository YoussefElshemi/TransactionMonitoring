using Core.ValueObjects;

namespace Core.Models.Usages;

public record CountUsageRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required DateRange FromDate { get; init; }
    public required DateRange ToDate { get; init; }
}