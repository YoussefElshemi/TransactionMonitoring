using Core.Models.Status;
using Core.ValueObjects;
using Presentation.Models.Status;

namespace Presentation.Mappers.Status;

public static class StatusSubscriptionMapper
{
    public static CreateStatusSubscriptionRequest ToModel(this CreateStatusSubscriptionRequestDto dto)
    {
        return new CreateStatusSubscriptionRequest
        {
            Email = new Email(dto.Body.Email)
        };
    }

    public static ConfirmStatusSubscriptionRequest ToModel(this ConfirmStatusSubscriptionRequestDto dto)
    {
        return new ConfirmStatusSubscriptionRequest
        {
            StatusSubscriptionId = new StatusSubscriptionId(dto.StatusSubscriptionId),
            Token = new StatusSubscriptionToken(dto.Token)
        };
    }

    public static UnsubscribeStatusSubscriptionRequest ToUnsubscribeModel(this UnsubscribeStatusSubscriptionRequestDto dto)
    {
        return new UnsubscribeStatusSubscriptionRequest
        {
            StatusSubscriptionId = new StatusSubscriptionId(dto.StatusSubscriptionId),
            Token = new StatusSubscriptionToken(dto.Token)
        };
    }

    public static StatusSubscriptionResponseDto ToDto(this IssuedStatusSubscription issuedStatusSubscription)
    {
        return new StatusSubscriptionResponseDto
        {
            StatusSubscriptionId = issuedStatusSubscription.StatusSubscription.StatusSubscriptionId,
            Message = "If that address can receive mail, a confirmation link is on its way to it."
        };
    }
}
