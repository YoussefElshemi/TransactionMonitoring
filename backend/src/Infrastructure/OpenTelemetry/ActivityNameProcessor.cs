using System.Diagnostics;
using Infrastructure.Constants;
using OpenTelemetry;

namespace Infrastructure.OpenTelemetry;

public class ActivityNameProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var method = activity.GetTagItem(OpenTelemetryTags.HttpRequestMethod);

        if (string.IsNullOrWhiteSpace(method?.ToString()))
        {
            return;
        }

        var path = activity.GetTagItem(OpenTelemetryTags.UrlPath);
        activity.DisplayName = $"{method} {path}";
        activity.SetTag(OpenTelemetryTags.ResourceName, activity.DisplayName);
    }
}