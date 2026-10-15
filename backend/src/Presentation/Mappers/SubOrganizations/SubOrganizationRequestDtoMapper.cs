using Core.Models.SubOrganizations;
using Core.ValueObjects;
using Presentation.Models.SubOrganizations;

namespace Presentation.Mappers.SubOrganizations;

public static class SubOrganizationRequestDtoMapper
{
    public static CreateSubOrganizationRequest ToModel(this CreateSubOrganizationRequestDto createSubOrganizationRequestDto)
    {
        return new CreateSubOrganizationRequest
        {
            OrganizationId = new OrganizationId(createSubOrganizationRequestDto.OrganizationId),
            Name = new SubOrganizationName(createSubOrganizationRequestDto.Body.Name),
            Description = !string.IsNullOrWhiteSpace(createSubOrganizationRequestDto.Body.Description)
                ? new SubOrganizationDescription(createSubOrganizationRequestDto.Body.Description)
                : null,
            ExternalReference = !string.IsNullOrWhiteSpace(createSubOrganizationRequestDto.Body.ExternalReference)
                ? new ExternalReference(createSubOrganizationRequestDto.Body.ExternalReference)
                : null,
            IsEnabled = new IsEnabled(createSubOrganizationRequestDto.Body.IsEnabled)
        };
    }

    public static UpdateSubOrganizationRequest ToModel(this UpdateSubOrganizationRequestDto updateSubOrganizationRequestDto)
    {
        return new UpdateSubOrganizationRequest
        {
            SubOrganizationId = new SubOrganizationId(updateSubOrganizationRequestDto.SubOrganizationId),
            Name = !string.IsNullOrWhiteSpace(updateSubOrganizationRequestDto.Body.Name)
                ? new SubOrganizationName(updateSubOrganizationRequestDto.Body.Name)
                : null,
            Description = !string.IsNullOrWhiteSpace(updateSubOrganizationRequestDto.Body.Description)
                ? new SubOrganizationDescription(updateSubOrganizationRequestDto.Body.Description)
                : null,
            ExternalReference = !string.IsNullOrWhiteSpace(updateSubOrganizationRequestDto.Body.ExternalReference)
                ? new ExternalReference(updateSubOrganizationRequestDto.Body.ExternalReference)
                : null,
            ClearExternalReference = new ClearCriteria(updateSubOrganizationRequestDto.Body.ClearExternalReference ?? false),
            IsEnabled = updateSubOrganizationRequestDto.Body.IsEnabled.HasValue
                ? new IsEnabled(updateSubOrganizationRequestDto.Body.IsEnabled.Value)
                : null
        };
    }

    public static ListSubOrganizationRequest ToModel(this ListSubOrganizationRequestDto listSubOrganizationRequestDto)
    {
        return new ListSubOrganizationRequest
        {
            OrganizationId = new OrganizationId(listSubOrganizationRequestDto.OrganizationId),
            Name = !string.IsNullOrWhiteSpace(listSubOrganizationRequestDto.Name)
                ? new SubOrganizationName(listSubOrganizationRequestDto.Name)
                : null,
            IsEnabled = listSubOrganizationRequestDto.IsEnabled.HasValue
                ? new IsEnabled(listSubOrganizationRequestDto.IsEnabled.Value)
                : null,
            PageNumber = new PageNumber(listSubOrganizationRequestDto.PageNumber),
            PageSize = new PageSize(listSubOrganizationRequestDto.PageSize),
            FromDate = new DateRange(listSubOrganizationRequestDto.FromDate),
            ToDate = new DateRange(listSubOrganizationRequestDto.ToDate)
        };
    }
}
