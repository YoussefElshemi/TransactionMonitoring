using Core.Enums;
using Presentation.Constants;

namespace Presentation.Extensions;

public static class ModeHeaderHelper
{
    public static Mode? Resolve(HttpContext? httpContext)
    {
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (httpContext.User.IsApiClient())
        {
            return httpContext.User.GetMode();
        }

        return TryParse(httpContext.Request.Headers[ModeConstants.ModeHeader], out var requested)
            ? requested
            : Mode.Live;
    }

    public static bool TryParse(string? value, out Mode mode)
    {
        mode = Mode.Live;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case ModeConstants.Live:
                mode = Mode.Live;

                return true;

            case ModeConstants.Test:
                mode = Mode.Test;

                return true;

            default:
                return false;
        }
    }

    public static string ToHeaderValue(Mode mode)
    {
        return mode == Mode.Test ? ModeConstants.Test : ModeConstants.Live;
    }
}
