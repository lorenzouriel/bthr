using System.Diagnostics;
using OpenTelemetry;

namespace bthr.Api.Telemetry;

// Run before export: neither SQL text nor raw URLs belong in shared telemetry.
public sealed class PrivacyProcessor(string databaseInstanceId) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name.StartsWith("Npgsql", StringComparison.Ordinal))
        {
            activity.SetTag("db.instance.id", databaseInstanceId);
            activity.DisplayName = "PostgreSQL " + (activity.GetTagItem("db.operation.name") ?? "query");
        }
        if (activity.Status == ActivityStatusCode.Error) activity.SetStatus(ActivityStatusCode.Error);
        foreach (var key in new[] { "db.statement", "db.query.text", "url.full", "url.query", "http.url", "http.target", "url.path", "exception.message", "exception.stacktrace" })
            activity.SetTag(key, null);
    }
}
