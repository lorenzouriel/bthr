using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using bthr.Api.Controllers;
using bthr.Api.Data;
using bthr.Api.Models;
using bthr.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bthr.Tests.IntegrationTests;

// Exercise real MVC routing, authorization, JSON binding, validation, and services.
// InMemory does not enforce PostgreSQL constraints; uniqueness is tested via service
// pre-checks here, with provider mappings and race-error translation tested separately.
public sealed class BodyTrackingEndpointTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private const string Root = "/api/users/1/body/";

    public BodyTrackingEndpointTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _host = new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IHabitService, HabitService>();
                services.AddScoped<IHabitLogService, HabitLogService>();
                services.AddScoped<ISubstanceLogService, SubstanceLogService>();
                services.AddScoped<ISymptomLogService, SymptomLogService>();
                services.AddControllers().AddApplicationPart(typeof(HabitsController).Assembly);
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Start();
        _client = _host.GetTestClient();
        _client.DefaultRequestHeaders.Add("X-Test-User", "1");

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Habits.AddRange(
            new Habit { Id = 10, UserId = 1, HabitName = "Read" },
            new Habit { Id = 20, UserId = 2, HabitName = "Other user" },
            new Habit { Id = 30, UserId = 1, HabitName = "Archived", Status = 0 });
        db.SaveChanges();
    }

    private static object Payload(string resource) => resource switch
    {
        "habits" => new { habitName = "Stretch", category = "Health" },
        "habit-logs" => new { habitId = 10, logDate = "2026-10-02", isCompleted = true },
        "substance-logs" => new { consumedAt = "2026-10-02T10:00:00Z", substanceType = "Caffeine", amount = 80.5m, unit = "mg" },
        "symptom-logs" => new { logDate = "2026-10-02", symptom = "Headache", severity = 3 },
        _ => throw new ArgumentOutOfRangeException(nameof(resource))
    };

    private async Task<JsonElement> CreateAsync(string resource, object? payload = null)
    {
        var response = await _client.PostAsJsonAsync(Root + resource, payload ?? Payload(resource));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [InlineData("habits")]
    [InlineData("habit-logs")]
    [InlineData("substance-logs")]
    [InlineData("symptom-logs")]
    public async Task CreateAndList_RoundTripAndIsolateUsers(string resource)
    {
        var created = await CreateAsync(resource);
        created.GetProperty("userId").GetInt32().Should().Be(1);
        created.GetProperty("status").GetInt32().Should().Be(1);
        created.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        var list = await _client.GetFromJsonAsync<JsonElement[]>(Root + resource);
        list.Should().Contain(row => row.GetProperty("id").GetInt32() == created.GetProperty("id").GetInt32());
        list.Should().OnlyContain(row => row.GetProperty("userId").GetInt32() == 1 && row.GetProperty("status").GetInt32() == 1);

        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", "2");
        var others = await _client.GetFromJsonAsync<JsonElement[]>("/api/users/2/body/" + resource);
        others.Should().NotContain(row => row.GetProperty("id").GetInt32() == created.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Habit_DefaultFrequencyIsDaily()
    {
        var created = await CreateAsync("habits");
        created.GetProperty("targetFrequency").GetString().Should().Be("Daily");
    }

    [Theory]
    [InlineData("habits")]
    [InlineData("habit-logs")]
    [InlineData("substance-logs")]
    [InlineData("symptom-logs")]
    public async Task CollectionEndpoints_RequireAuthenticationAndMatchingUser(string resource)
    {
        (await _client.GetAsync("/api/users/2/body/" + resource)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.PostAsJsonAsync("/api/users/2/body/" + resource, Payload(resource))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        (await _client.GetAsync(Root + resource)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.PostAsJsonAsync(Root + resource, Payload(resource))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("habits", "{}")]
    [InlineData("habits", "{\"habitName\":\" \"}")]
    [InlineData("habits", "{\"habitName\":\"Read\",\"targetFrequency\":\"\"}")]
    [InlineData("habit-logs", "{\"habitId\":10}")]
    [InlineData("habit-logs", "{\"logDate\":\"2026-10-02\"}")]
    [InlineData("habit-logs", "{\"habitId\":0,\"logDate\":\"2026-10-02\"}")]
    [InlineData("habit-logs", "{\"habitId\":10,\"logDate\":\"invalid\"}")]
    [InlineData("substance-logs", "{}")]
    [InlineData("substance-logs", "{\"consumedAt\":\"2026-10-02T10:00:00Z\",\"substanceType\":\"Caffeine\",\"unit\":\"mg\"}")]
    [InlineData("substance-logs", "{\"consumedAt\":\"2026-10-02T10:00:00Z\",\"substanceType\":\"Caffeine\",\"unit\":\"mg\",\"amount\":10000}")]
    [InlineData("substance-logs", "{\"consumedAt\":\"2026-10-02T10:00:00Z\",\"substanceType\":\"Caffeine\",\"unit\":\"mg\",\"amount\":-1}")]
    [InlineData("symptom-logs", "{\"symptom\":\"Headache\"}")]
    [InlineData("symptom-logs", "{\"logDate\":\"2026-10-02\"}")]
    [InlineData("symptom-logs", "{\"logDate\":\"2026-10-02\",\"symptom\":\"Headache\",\"severity\":0}")]
    [InlineData("symptom-logs", "{\"logDate\":\"2026-10-02\",\"symptom\":\"Headache\",\"severity\":6}")]
    public async Task Create_RejectsInvalidPayload(string resource, string json)
    {
        var response = await _client.PostAsync(Root + resource, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(999)]
    public async Task HabitLog_RejectsForeignArchivedOrMissingHabit(int habitId)
    {
        var response = await _client.PostAsJsonAsync(Root + "habit-logs", new { habitId, logDate = "2026-10-02" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("habits")]
    [InlineData("habit-logs")]
    public async Task DuplicateRecords_ConflictEvenAfterSoftDeletion(string resource)
    {
        var created = await CreateAsync(resource);
        (await _client.PostAsJsonAsync(Root + resource, Payload(resource))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var id = created.GetProperty("id").GetInt32();
        (await _client.DeleteAsync(Root + resource + "/" + id)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync(Root + resource, Payload(resource))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("habits")]
    [InlineData("habit-logs")]
    public async Task UpdateAndDelete_CheckOwnershipAndSoftDelete(string resource)
    {
        var created = await CreateAsync(resource);
        var id = created.GetProperty("id").GetInt32();
        var path = Root + resource + "/" + id;
        object update = resource == "habits" ? new { habitName = "Updated" } : new { isCompleted = false };

        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", "2");
        (await _client.PutAsJsonAsync(path, update)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.DeleteAsync(path)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var foreignPath = "/api/users/2/body/" + resource + "/" + id;
        (await _client.PutAsJsonAsync(foreignPath, update)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.DeleteAsync(foreignPath)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", "1");
        var response = await _client.PutAsJsonAsync(path, update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (resource == "habits") updated.GetProperty("habitName").GetString().Should().Be("Updated");
        else updated.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        (await _client.DeleteAsync(path)).StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await _client.GetFromJsonAsync<JsonElement[]>(Root + resource);
        list.Should().NotContain(row => row.GetProperty("id").GetInt32() == id);
        (await _client.PutAsJsonAsync(path, update)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync(path)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PutAsJsonAsync(Root + resource + "/999", update)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync(Root + resource + "/999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var status = resource == "habits" ? (await db.Habits.FindAsync(id))!.Status : (await db.HabitLogs.FindAsync(id))!.Status;
        status.Should().Be(0);
    }

    [Theory]
    [InlineData("habits", "{\"habitName\":\"\"}")]
    [InlineData("habits", "{\"status\":2}")]
    [InlineData("habit-logs", "{\"habitId\":0}")]
    [InlineData("habit-logs", "{\"status\":-1}")]
    public async Task Update_RejectsInvalidPayload(string resource, string json)
    {
        var created = await CreateAsync(resource);
        var response = await _client.PutAsync(Root + resource + "/" + created.GetProperty("id").GetInt32(),
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HabitRename_RejectsDuplicateWithoutChangingOriginal()
    {
        var created = await CreateAsync("habits");
        var response = await _client.PutAsJsonAsync(Root + "habits/" + created.GetProperty("id").GetInt32(), new { habitName = "Read" });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var list = await _client.GetFromJsonAsync<JsonElement[]>(Root + "habits");
        list.Should().Contain(row => row.GetProperty("habitName").GetString() == "Stretch");
    }

    [Fact]
    public async Task HabitLog_UpdateChecksParentAndUniqueness()
    {
        await CreateAsync("habit-logs");
        var second = await CreateAsync("habit-logs", new { habitId = 10, logDate = "2026-10-03" });
        var path = Root + "habit-logs/" + second.GetProperty("id").GetInt32();
        (await _client.PutAsJsonAsync(path, new { habitId = 20 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _client.PutAsJsonAsync(path, new { habitId = 30 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _client.PutAsJsonAsync(path, new { habitId = 999 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _client.PutAsJsonAsync(path, new { logDate = "2026-10-02" })).StatusCode.Should().Be(HttpStatusCode.Conflict);
        // Updating a record without changing its unique key must succeed.
        (await _client.PutAsJsonAsync(path, new { notes = "Done" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.DeleteAsync(Root + "habits/10")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PutAsJsonAsync(path, new { isCompleted = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await _client.GetFromJsonAsync<JsonElement[]>(Root + "habit-logs?habit_id=10");
        logs.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("habit-logs", "2026-10-02")]
    [InlineData("symptom-logs", "2026-10-02")]
    [InlineData("substance-logs", "2026-10-02T10:00:00Z")]
    public async Task DateFilters_AreInclusiveAndRejectReversedRanges(string resource, string date)
    {
        await CreateAsync(resource);
        var match = await _client.GetFromJsonAsync<JsonElement[]>(Root + resource + "?start_date=" + date + "&end_date=" + date);
        match.Should().HaveCount(1);
        var empty = await _client.GetFromJsonAsync<JsonElement[]>(Root + resource + "?start_date=2026-10-03");
        empty.Should().BeEmpty();
        empty = await _client.GetFromJsonAsync<JsonElement[]>(Root + resource + "?end_date=2026-10-01");
        empty.Should().BeEmpty();
        (await _client.GetAsync(Root + resource + "?start_date=2026-10-03&end_date=2026-10-01"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        if (resource == "habit-logs")
        {
            (await _client.GetFromJsonAsync<JsonElement[]>(Root + resource + "?habit_id=20")).Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("substance-logs")]
    [InlineData("symptom-logs")]
    public async Task AppendOnlyLogs_DoNotExposeMutationRoutes(string resource)
    {
        var created = await CreateAsync(resource);
        var path = Root + resource + "/" + created.GetProperty("id").GetInt32();
        (await _client.PutAsJsonAsync(path, Payload(resource))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync(path)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(null)]
    public async Task SymptomSeverity_AcceptsBoundsAndNull(int? severity)
    {
        await CreateAsync("symptom-logs", new { logDate = "2026-10-02", symptom = "Headache", severity });
    }

    [Fact]
    public async Task SubstanceTimestamp_PreservesTheInstantWithAnExplicitOffset()
    {
        var created = await CreateAsync("substance-logs", new
        {
            consumedAt = "2026-10-02T13:00:00+03:00",
            substanceType = "Caffeine", amount = 80, unit = "mg"
        });
        created.GetProperty("consumedAt").GetDateTime()
            .Should().Be(new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc));
        var date = Uri.EscapeDataString("2026-10-02T13:00:00+03:00");
        (await _client.GetFromJsonAsync<JsonElement[]>(Root + "substance-logs?start_date=" + date + "&end_date=" + date))
            .Should().HaveCount(1);
    }

    [Theory]
    [InlineData("habits")]
    [InlineData("habit-logs")]
    [InlineData("substance-logs")]
    [InlineData("symptom-logs")]
    public async Task InvalidIdentity_CannotMatchASentinelUserId(string resource)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", "invalid");
        (await _client.GetAsync("/api/users/-1/body/" + resource)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var userId))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
        }
    }
}
