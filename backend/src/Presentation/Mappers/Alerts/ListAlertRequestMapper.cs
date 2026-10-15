using Core.Enums;
using Core.Models.Alerts;
using Core.ValueObjects;
using Presentation.Models.Alerts;

namespace Presentation.Mappers.Alerts;

public static class ListAlertRequestMapper
{
    public static ListAlertRequest ToModel(this ListAlertRequestDto listAlertRequestDto)
    {
        return new ListAlertRequest
        {
            OrganizationId = new OrganizationId(listAlertRequestDto.OrganizationId),
            FriendlyName = listAlertRequestDto.FriendlyName != null ? new FriendlyName(listAlertRequestDto.FriendlyName) : null,
            Status = listAlertRequestDto.Status != null ? Enum.Parse<AlertStatus>(listAlertRequestDto.Status, true) : null,
            Severity = listAlertRequestDto.Severity != null ? Enum.Parse<RuleSeverity>(listAlertRequestDto.Severity, true) : null,
            RuleId = listAlertRequestDto.RuleId.HasValue ? new RuleId(listAlertRequestDto.RuleId.Value) : null,
            PartyId = listAlertRequestDto.PartyId.HasValue ? new PartyId(listAlertRequestDto.PartyId.Value) : null,
            CaseId = listAlertRequestDto.CaseId.HasValue ? new CaseId(listAlertRequestDto.CaseId.Value) : null,
            PageNumber = new PageNumber(listAlertRequestDto.PageNumber),
            PageSize = new PageSize(listAlertRequestDto.PageSize),
            FromDate = new DateRange(listAlertRequestDto.FromDate),
            ToDate = new DateRange(listAlertRequestDto.ToDate)
        };
    }
}
