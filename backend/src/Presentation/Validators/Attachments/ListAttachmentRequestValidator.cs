using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Attachments;
using Presentation.Models.Attachments;
using Presentation.Validators.Common;

namespace Presentation.Validators.Attachments;

public class ListAttachmentRequestValidator(
    IAttachmentService attachmentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListAttachmentRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListAttachmentRequestDto request, CancellationToken stoppingToken)
    {
        return attachmentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
