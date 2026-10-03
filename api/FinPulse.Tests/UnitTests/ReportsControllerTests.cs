using System.Security.Claims;
using FinPulse.Api.Controllers;
using FinPulse.Api.Data;
using FinPulse.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinPulse.Tests.UnitTests;

public class ReportsControllerTests
{
    [Fact]
    public void MissingValuesRemainNullAndExplicitZeroIsPreserved()
    {
        var start = new DateTime(2026, 1, 8);
        var rows = new[] { new ReportsController.Observation(1, start.AddDays(-1), 0),
            new ReportsController.Observation(2, start, null) };
        var result = ReportsController.Summarize("test", "test", "test", "ml", rows, start, start.AddDays(7));
        Assert.Null(result.Value);
        Assert.Equal(0, result.PreviousValue);
        Assert.Equal(0, result.Count);
        Assert.Equal(1, result.PreviousCount);
    }

    [Fact]
    public void PeriodsDoNotOverlapAndAveragesExcludeMissingValues()
    {
        var start = new DateTime(2026, 1, 8);
        var rows = new[] { new ReportsController.Observation(1, start.AddDays(-1), 3),
            new ReportsController.Observation(2, start, 6), new ReportsController.Observation(3, start.AddDays(6).AddHours(23), 8),
            new ReportsController.Observation(4, start.AddDays(7), 99), new ReportsController.Observation(5, start, null) };
        var result = ReportsController.Summarize("test", "test", "test", "hours", rows, start, start.AddDays(7), true);
        Assert.Equal(7, result.Value);
        Assert.Equal(3, result.PreviousValue);
        Assert.Equal(new[] { 2, 3 }, result.RecordIds);
    }

    [Fact]
    public async Task ReportScopesDataToSessionAndKeepsCurrenciesSeparate()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Earnings.AddRange(new Earning { Id = 1, UserId = 7, Status = 1, CurrencyCode = "BRL", Amount = 10, EarningDate = new DateTime(2026, 1, 8) },
            new Earning { Id = 2, UserId = 8, Status = 1, CurrencyCode = "BRL", Amount = 900, EarningDate = new DateTime(2026, 1, 8) },
            new Earning { Id = 3, UserId = 7, Status = 1, CurrencyCode = "USD", Amount = 20, EarningDate = new DateTime(2026, 1, 8) });
        await db.SaveChangesAsync();
        var controller = new ReportsController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "test")) } } };
        var response = Assert.IsType<OkObjectResult>(await controller.Review(new(2026, 1, 8), new(2026, 1, 14), "finance"));
        var metrics = (List<ReportsController.Metric>)response.Value!.GetType().GetProperty("metrics")!.GetValue(response.Value)!;
        Assert.Equal(10, metrics.Single(m => m.Key == "earnings-BRL").Value);
        Assert.Equal(20, metrics.Single(m => m.Key == "earnings-USD").Value);
        Assert.DoesNotContain(metrics.SelectMany(m => m.RecordIds), id => id == 2);
        Assert.IsType<BadRequestObjectResult>(await controller.Review(new(2026, 1, 8), new(2026, 1, 14), "all"));
    }
}
