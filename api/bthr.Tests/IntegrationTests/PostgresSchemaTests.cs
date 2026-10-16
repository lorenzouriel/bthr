using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using bthr.Api.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace bthr.Tests.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BTHR_TEST_POSTGRES")))
            Skip = "Set BTHR_TEST_POSTGRES to a disposable PostgreSQL server with CREATE DATABASE permission.";
    }
}

public class PostgresSchemaTests
{
    [PostgresFact]
    public async Task SqlMigrations_SupportProductionAuthenticationAndEveryResource()
    {
        var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BTHR_TEST_POSTGRES"));
        var database = "bthr_schema_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {database}", admin).ExecuteNonQueryAsync();
        try
        {
            settings.Database = database;
            settings.Pooling = false;
            await using (var connection = new NpgsqlConnection(settings.ConnectionString))
            {
                await connection.OpenAsync();
                var migrations = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Migrations"), "V*.sql")
                    .OrderBy(path => int.Parse(Path.GetFileName(path).Split('_')[0][1..])).ToArray();
                migrations.Should().HaveCount(23);
                foreach (var migration in migrations)
                    await new NpgsqlCommand(await File.ReadAllTextAsync(migration), connection).ExecuteNonQueryAsync();

                // Compare every persisted EF property with the actual migrated PostgreSQL schema.
                await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(settings.ConnectionString).Options);
                foreach (var entity in db.Model.GetEntityTypes())
                {
                    var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                    await using var command = new NpgsqlCommand("""
                        SELECT a.attname, format_type(a.atttypid, a.atttypmod), a.attnotnull
                        FROM pg_attribute a
                        JOIN pg_class c ON c.oid = a.attrelid
                        JOIN pg_namespace n ON n.oid = c.relnamespace
                        WHERE n.nspname = @schema AND c.relname = @table AND a.attnum > 0 AND NOT a.attisdropped
                        """, connection);
                    command.Parameters.AddWithValue("schema", entity.GetSchema() ?? "public");
                    command.Parameters.AddWithValue("table", entity.GetTableName()!);
                    var columns = new Dictionary<string, (string Type, bool Required)>();
                    await using (var reader = await command.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                            columns.Add(reader.GetString(0), (reader.GetString(1), reader.GetBoolean(2)));
                    entity.GetProperties().Select(p => p.GetColumnName(table)).Should().BeEquivalentTo(columns.Keys,
                        $"{entity.Name} must map exactly the columns in the SQL migrations");
                    foreach (var property in entity.GetProperties())
                    {
                        var column = columns[property.GetColumnName(table)!];
                        Normalize(property.GetColumnType()!).Should().Be(Normalize(column.Type), $"{entity.Name}.{property.Name}");
                        property.IsNullable.Should().Be(!column.Required, $"{entity.Name}.{property.Name}");
                    }
                }
            }

            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", settings.ConnectionString);
                builder.UseSetting("Otel:ExporterEndpoint", "http://127.0.0.1:1");
                builder.UseSetting("Jwt:SecretKey", "isolated-schema-test-signing-key-at-least-32-bytes");
                builder.UseSetting("Jwt:Issuer", "schema-tests");
                builder.UseSetting("Jwt:Audience", "schema-tests");
                builder.UseSetting("Cookie:Secure", "false");
            });
            using var client = factory.CreateClient();
            var registered = await Send(client, HttpMethod.Post, "/api/auth/register", new
            {
                username = "schema_test", email = "schema@example.test", phoneNumber = "+551100000001",
                password = "Schema-test-123!"
            }, HttpStatusCode.Created);
            var userId = registered.GetProperty("userId").GetInt32();
            var root = $"/api/users/{userId}/";
            var me = await Send(client, HttpMethod.Get, "/api/auth/me", null);
            me.TryGetProperty("plan", out _).Should().BeFalse();
            await Send(client, HttpMethod.Post, "/api/auth/logout", null);
            await Send(client, HttpMethod.Post, "/api/auth/login", new { email = "schema@example.test", password = "Schema-test-123!" });

            var habit = await Send(client, HttpMethod.Post, root + "body/habits", new { habitName = "Read", targetFrequency = "Daily" }, HttpStatusCode.Created);
            var records = new (string Path, object Payload)[]
            {
                ("expenses", new { amount = 10, category = "Food", paymentMethod = "Cash", currencyCode = "BRL", expenseDate = "2026-10-14" }),
                ("earnings", new { amount = 100, category = "Salary", paymentMethod = "Transfer", currencyCode = "BRL", earningDate = "2026-10-14" }),
                ("budgets", new { name = "Food", amountLimit = 100, currencyCode = "BRL", startDate = "2026-10-01", endDate = "2026-10-31" }),
                ("goals", new { name = "Savings", targetAmount = 1000, currentAmount = 10, currencyCode = "BRL", dueDate = "2026-12-31" }),
                ("investments", new { assetName = "Fund", investmentType = "Fund", category = "Savings", investedAmount = 50, currencyCode = "BRL", purchaseDate = "2026-10-14" }),
                ("bills", new { name = "Rent", category = "Housing", amount = 100, currencyCode = "BRL", dueDate = "2026-10-14", recurrenceType = "Monthly", recurrenceInterval = 1, nextDueDate = "2026-11-14", paidDate = "2026-10-13" }),
                ("body/weekly-routines", new { dayOfWeek = 1, routineName = "Walk" }),
                ("body/workouts", new { workoutDate = "2026-10-14", routineName = "Walk", durationMinutes = 30 }),
                ("body/personal-records", new { exerciseName = "Walk", metricType = "Distance", value = 5, unit = "km", achievedDate = "2026-10-14" }),
                ("body/meals", new { mealDate = "2026-10-14", mealType = "Lunch", calories = 500 }),
                ("body/water-intake", new { intakeDate = "2026-10-14", amountMl = 500 }),
                ("body/body-metrics", new { measuredDate = "2026-10-14", weightKg = 70 }),
                ("body/sleep-logs", new { bedTime = "2026-10-13T23:00:00Z", wakeTime = "2026-10-14T07:00:00Z" }),
                ("body/habit-logs", new { habitId = habit.GetProperty("id").GetInt32(), logDate = "2026-10-14", isCompleted = true }),
                ("body/substance-logs", new { consumedAt = "2026-10-14T08:00:00Z", substanceType = "Caffeine", amount = 80, unit = "mg" }),
                ("body/symptom-logs", new { logDate = "2026-10-14", symptom = "Headache", severity = 1 }),
                ("mind/meditation-sessions", new { sessionDate = "2026-10-14", durationMinutes = 10, meditationType = "Breathing" }),
                ("mind/journal-entries", new { entryDate = "2026-10-14", content = "Test reflection", mood = 4 })
            };
            foreach (var (path, payload) in records)
            {
                var created = await Send(client, HttpMethod.Post, root + path, payload, HttpStatusCode.Created);
                var listed = await Send(client, HttpMethod.Get, root + path, null);
                listed.GetArrayLength().Should().Be(1, path);
                created.GetProperty("status").GetInt32().Should().Be(1, path);
                listed[0].GetProperty("status").GetInt32().Should().Be(1, path);
                created.GetProperty("createdAt").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-5), path);
                // PostgreSQL timestamps store microseconds; DateTime also carries 100 ns ticks.
                listed[0].GetProperty("createdAt").GetDateTime().Should().BeCloseTo(created.GetProperty("createdAt").GetDateTime(), TimeSpan.FromMicroseconds(1), path);
                if (path == "body/sleep-logs") created.GetProperty("totalHours").GetDecimal().Should().Be(8);
                if (path == "bills")
                {
                    var billPath = root + path + "/" + created.GetProperty("id").GetInt32();
                    created.GetProperty("paidDate").GetString().Should().Be("2026-10-13");
                    var cleared = await Send(client, HttpMethod.Put, billPath, new { paidDate = (string?)null, nextDueDate = (string?)null });
                    cleared.GetProperty("paidDate").ValueKind.Should().Be(JsonValueKind.Null);
                    cleared.GetProperty("nextDueDate").ValueKind.Should().Be(JsonValueKind.Null);
                    cleared.GetProperty("dueDate").GetString().Should().Be("2026-10-14");
                    cleared.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.String);
                    var filtered = await Send(client, HttpMethod.Get, root + path + "?year=2026&month=10", null);
                    filtered.GetArrayLength().Should().Be(1);
                    await Send(client, HttpMethod.Delete, billPath, null);
                    (await Send(client, HttpMethod.Get, root + path, null)).GetArrayLength().Should().Be(0);
                }
            }
            (await Send(client, HttpMethod.Get, root + "body/habits", null)).GetArrayLength().Should().Be(1);
            await Send(client, HttpMethod.Post, root + "body/water-intake", new { intakeDate = "2026-10-14", amountMl = 100 }, HttpStatusCode.Conflict);
            await Send(client, HttpMethod.Post, root + "body/sleep-logs", new { bedTime = "2026-10-14T08:00:00Z", wakeTime = "2026-10-14T07:00:00Z" }, HttpStatusCode.BadRequest);
            await Send(client, HttpMethod.Post, root + "expenses", new { amount = 10, category = new string('x', 101), paymentMethod = "Cash", currencyCode = "BRL", expenseDate = "2026-10-14" }, HttpStatusCode.BadRequest);
            await Send(client, HttpMethod.Get, "/api/reports/review?start_date=2026-10-01&end_date=2026-10-31&domain=all&combine=true", null);
        }
        finally
        {
            // Only the uniquely named database created above is removed.
            await new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
    }

    private static string Normalize(string type) => type.Replace("decimal", "numeric").Replace("varchar", "character varying");

    private static async Task<JsonElement> Send(HttpClient client, HttpMethod method, string path, object? body,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, $"{method} {path}: {text}");
        return JsonSerializer.Deserialize<JsonElement>(text);
    }
}
