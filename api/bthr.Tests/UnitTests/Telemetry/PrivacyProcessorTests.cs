using System.Diagnostics;
using bthr.Api.Telemetry;
using FluentAssertions;

namespace bthr.Tests.UnitTests.Telemetry;

public class PrivacyProcessorTests
{
    [Fact]
    public void DatabaseSpansKeepCorrelationButRemoveSqlAndErrorContent()
    {
        using var source = new ActivitySource("Npgsql");
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == "Npgsql",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var span = source.StartActivity("SELECT private journal text")!;
        span.SetTag("db.statement", "secret");
        span.SetTag("db.query.text", "secret");
        span.SetTag("url.full", "https://example/api/users/12?token=secret");
        span.SetTag("db.namespace", "bthr");
        span.SetStatus(ActivityStatusCode.Error, "private data");
        var traceId = span.TraceId;
        new PrivacyProcessor("postgres-test").OnEnd(span);
        span.GetTagItem("db.statement").Should().BeNull();
        span.GetTagItem("db.query.text").Should().BeNull();
        span.GetTagItem("url.full").Should().BeNull();
        span.StatusDescription.Should().BeNullOrEmpty();
        span.GetTagItem("db.instance.id").Should().Be("postgres-test");
        span.GetTagItem("db.namespace").Should().Be("bthr");
        span.DisplayName.Should().Be("PostgreSQL query");
        span.TraceId.Should().Be(traceId);
    }
}
