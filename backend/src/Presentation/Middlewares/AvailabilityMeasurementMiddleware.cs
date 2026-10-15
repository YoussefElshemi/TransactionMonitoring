using System.Diagnostics;
using Presentation.Availability;
using Presentation.Extensions;

namespace Presentation.Middlewares;

public class AvailabilityMeasurementMiddleware(RequestDelegate next, AvailabilityRecorder availabilityRecorder, TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            Record(context, stopwatch.ElapsedMilliseconds);
        }
    }

    private void Record(HttpContext context, long elapsedMilliseconds)
    {
        if (!context.User.IsApiClient())
        {
            return;
        }

        Guid organizationId;

        try
        {
            organizationId = context.User.GetOrganizationId();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        availabilityRecorder.Record(
            organizationId,
            timeProvider.GetUtcNow(),
            elapsedMilliseconds,
            IsPlatformFailure(context.Response.StatusCode));
    }

    private static bool IsPlatformFailure(int statusCode)
    {
        return statusCode >= StatusCodes.Status500InternalServerError;
    }
}
