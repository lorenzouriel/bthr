using System.Security.Claims;
using System.Text.Json;
using bthr.Api.Controllers;
using bthr.Api.Data;
using bthr.Api.Models;
using bthr.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace bthr.Tests.UnitTests.Services;

public class TelegramAgentTests
{
    private const string Key = "test-service-key-longer-than-thirty-two-characters";
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Bot:ApiKey"] = Key, ["Bot:Username"] = "bthr_test_bot"
    }).Build();
    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static TelegramController Controller(ApplicationDbContext db, TelegramLinks links, int userId = 1)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        http.Request.Headers["X-Bthr-Link"] = "1";
        http.Request.Headers["X-Bot-Api-Key"] = Key;
        return new(db, links, Config()) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    [Fact]
    public void CatalogContainsOnlyNineteenOwnerScopedResources()
    {
        Assert.Equal(19, AgentCatalog.Resources.Count);
        Assert.DoesNotContain("users", AgentCatalog.Resources.Keys);
        foreach (var resource in AgentCatalog.Resources.Values)
        {
            Assert.StartsWith("/api/users/{userId}/", resource.Path);
            var schema = JsonSerializer.SerializeToElement(AgentCatalog.Schema(resource.Create));
            Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
            Assert.DoesNotContain(schema.GetProperty("properties").EnumerateObject(), p => p.Name == "userId");
        }
    }

    [Fact]
    public void ChallengeCannotSwitchSenderOrSurviveReplacement()
    {
        var links = new TelegramLinks();
        var token = links.Create(1);
        Assert.True(links.Claim(token, 100));
        Assert.False(links.Claim(token, 200));
        links.Create(1);
        Assert.Null(links.Get(token));
        Assert.False(links.Claim(token, 100));
    }

    [Fact]
    public async Task ClaimDoesNotLinkUntilOwnerApprovesAndCannotBeReplayed()
    {
        using var db = Database();
        db.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@example.test" });
        await db.SaveChangesAsync();
        var links = new TelegramLinks();
        var controller = Controller(db, links);
        var token = links.Create(1);
        Assert.IsType<OkResult>(await controller.Claim(new(token, 100)));
        Assert.Null((await db.Users.FindAsync(1))!.TelegramUserId);
        Assert.IsType<BadRequestResult>(await Controller(db, links, 2).Approve(new(token)));
        Assert.IsType<OkResult>(await controller.Approve(new(token)));
        Assert.Equal(100, (await db.Users.FindAsync(1))!.TelegramUserId);
        Assert.IsType<BadRequestResult>(await controller.Approve(new(token)));
        Assert.IsType<OkResult>(await controller.Unlink());
        Assert.Null((await db.Users.FindAsync(1))!.TelegramUserId);
    }

    [Fact]
    public async Task ApprovalRejectsAlreadyLinkedSenderAndMissingCsrfHeader()
    {
        using var db = Database();
        db.Users.AddRange(new User { Id = 1 }, new User { Id = 2, TelegramUserId = 100 });
        await db.SaveChangesAsync();
        var links = new TelegramLinks();
        var token = links.Create(1); links.Claim(token, 100);
        var controller = Controller(db, links);
        Assert.IsType<ConflictObjectResult>(await controller.Approve(new(token)));
        controller.Request.Headers.Remove("X-Bthr-Link");
        Assert.IsType<ForbidResult>(await controller.Approve(new(token)));
    }

    [Fact]
    public void PhoneChallengeNeedsMatchingProfileAndExplicitApproval()
    {
        var links = new TelegramLinks();
        var token = links.CreatePhone(100, "5511999999999");
        Assert.False(links.AttachPhone(token, 1, "5511888888888"));
        Assert.True(links.AttachPhone(token, 1, "+55 (11) 99999-9999"));
        Assert.False(links.AttachPhone(token, 2, "5511999999999"));
        Assert.Equal(1, links.Get(token)!.UserId);
    }

    [Fact]
    public void ServiceKeyComparisonFailsClosed()
    {
        var request = new DefaultHttpContext().Request;
        Assert.False(TelegramLinks.ServiceKey(request, Config()));
        request.Headers["X-Bot-Api-Key"] = "wrong";
        Assert.False(TelegramLinks.ServiceKey(request, Config()));
        request.Headers["X-Bot-Api-Key"] = Key;
        Assert.True(TelegramLinks.ServiceKey(request, Config()));
    }

    [Fact]
    public async Task ServiceAuthenticationRechecksRevocationAndDeniesAdminRoutes()
    {
        using var db = Database();
        var user = new User { Id = 1, TelegramUserId = 100 };
        db.Users.Add(user); await db.SaveChangesAsync();
        async Task<bool> Authenticate(string path)
        {
            var options = new Moq.Mock<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>>();
            options.Setup(x => x.Get(Moq.It.IsAny<string>())).Returns(new Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions());
            using var logger = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
            var handler = new TelegramAuthentication(options.Object, logger, System.Text.Encodings.Web.UrlEncoder.Default, db, Config());
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Headers["X-Bot-Api-Key"] = Key;
            context.Request.Headers["X-Telegram-User"] = "100";
            await handler.InitializeAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme("telegram", null, typeof(TelegramAuthentication)), context);
            return (await handler.AuthenticateAsync()).Succeeded;
        }
        Assert.True(await Authenticate("/api/users/1/body/meals"));
        Assert.False(await Authenticate("/api/users/2/body/meals"));
        Assert.False(await Authenticate("/api/users"));
        Assert.False(await Authenticate("/api/auth/change-password"));
        user.TelegramUserId = null; await db.SaveChangesAsync();
        Assert.False(await Authenticate("/api/users/1/body/meals"));
    }
}
