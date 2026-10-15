using Core.Models.Attachments;
using Core.ValueObjects;
using Presentation.Models.Attachments;

namespace Presentation.Mappers.Attachments;

public static class ListAttachmentRequestMapper
{
    public static ListAttachmentRequest ToModel(this ListAttachmentRequestDto listAttachmentRequestDto)
    {
        return new ListAttachmentRequest
        {
            CaseId = new CaseId(listAttachmentRequestDto.CaseId),
            OrganizationId = new OrganizationId(listAttachmentRequestDto.OrganizationId),
            FileName = string.IsNullOrWhiteSpace(listAttachmentRequestDto.FileName)
                ? null
                : new FileName(listAttachmentRequestDto.FileName),
            ScanStatus = listAttachmentRequestDto.ScanStatus,
            PageNumber = new PageNumber(listAttachmentRequestDto.PageNumber),
            PageSize = new PageSize(listAttachmentRequestDto.PageSize),
            FromDate = new DateRange(listAttachmentRequestDto.FromDate),
            ToDate = new DateRange(listAttachmentRequestDto.ToDate)
        };
    }
}
