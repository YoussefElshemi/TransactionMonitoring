using Core.Models.Sandbox;
using Presentation.Models.Sandbox;

namespace Presentation.Mappers.Sandbox;

public static class SandboxDtoMapper
{
    public static SandboxCreditResponseDto ToDto(this SandboxCredit sandboxCredit)
    {
        return new SandboxCreditResponseDto
        {
            OrganizationId = sandboxCredit.OrganizationId,
            Allowance = sandboxCredit.Allowance,
            Used = sandboxCredit.Used,
            Remaining = sandboxCredit.Remaining,
            ResetsAt = sandboxCredit.ResetsAt,
            Unit = "rule-pass"
        };
    }

    public static SandboxResetResponseDto ToDto(this SandboxReset sandboxReset)
    {
        return new SandboxResetResponseDto
        {
            OrganizationId = sandboxReset.OrganizationId,
            Mode = sandboxReset.Mode.ToString(),
            Counts = sandboxReset.Counts.ConvertAll(x => new SandboxResetCountDto
            {
                Resource = x.Resource.ToString(),
                Count = x.Count
            }).ToArray(),
            TotalDeleted = sandboxReset.TotalDeleted,
            ResetAt = sandboxReset.ResetAt
        };
    }
}
