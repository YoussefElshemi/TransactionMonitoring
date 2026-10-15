using System.Net;

namespace Core.Exceptions;

public class InvalidOffboardingException(string message) : DomainException(message)
{
    public static InvalidOffboardingException Self()
    {
        return new InvalidOffboardingException(
            "You cannot offboard yourself. Somebody else on the desk has to take the work first.");
    }

    public static InvalidOffboardingException SuccessorIsLeaver()
    {
        return new InvalidOffboardingException("The successor cannot be the person being offboarded.");
    }

    public static InvalidOffboardingException SuccessorOutsideOrganization()
    {
        return new InvalidOffboardingException("The successor has to belong to the same organisation.");
    }

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Offboarding is not valid";
    }
}
