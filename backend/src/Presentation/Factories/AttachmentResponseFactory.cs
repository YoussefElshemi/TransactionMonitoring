using Core.Models.Attachments;
using Core.ValueObjects;
using Presentation.Mappers.Attachments;
using Presentation.Models.Attachments;

namespace Presentation.Factories;

public class AttachmentResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<AttachmentResourceDto> CreateAsync(Attachment attachment, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([attachment], context, stoppingToken);

        return resources[0];
    }

    public async Task<AttachmentResourceDto[]> CreateAsync(
        IReadOnlyCollection<Attachment> attachments, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (attachments.Count == 0)
        {
            return [];
        }

        var uploadedByUsers = await relatedRecordLoader.LoadUsersAsync(
            attachments.Select(x => (ApplicationUserId?)x.UploadedByUserId), context, stoppingToken);

        return attachments.Select(x => new AttachmentResourceDto
        {
            Attachment = x.ToDto(),
            UploadedByUser = uploadedByUsers.GetValueOrDefault(x.UploadedByUserId)
        }).ToArray();
    }
}
