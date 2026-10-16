using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using bthr.Api.Data;

namespace bthr.Api.Services;

// No long-lived impersonation JWT. Resolve and recheck the binding on every call.
public sealed class TelegramAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, ApplicationDbContext db, IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TelegramLinks.ServiceKey(Request, config) || !long.TryParse(Request.Headers["X-Telegram-User"], out var sender) || sender <= 0)
            return AuthenticateResult.Fail("Invalid service identity");
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.TelegramUserId == sender && x.Status == 1);
        if (user == null) return AuthenticateResult.Fail("Unlinked account");
        // Restrict the service principal to domain data, reports, media and its own unlink/status.
        var path = Request.Path.Value ?? "";
        if (!(path.StartsWith($"/api/users/{user.Id}/", StringComparison.Ordinal) || path.StartsWith("/api/agent/", StringComparison.Ordinal) || path == "/api/reports/review" ||
            path.StartsWith("/api/meal-photos/", StringComparison.Ordinal) || path == "/api/telegram/status" || path == "/api/telegram/unlink"))
            return AuthenticateResult.Fail("Service route denied");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("telegram", sender.ToString()), new Claim("admin", "false") };
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
