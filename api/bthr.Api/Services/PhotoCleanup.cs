using bthr.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace bthr.Api.Services;

public sealed class PhotoCleanup(IServiceScopeFactory scopes, IConfiguration config, ILogger<PhotoCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var root = Path.GetFullPath(config["Photos:Root"] ?? Path.Combine(AppContext.BaseDirectory, "private-photos"));
                if (!Directory.Exists(root)) continue;
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                foreach (var userDir in Directory.EnumerateDirectories(root))
                {
                    if (!int.TryParse(Path.GetFileName(userDir), out var userId) || (File.GetAttributes(userDir) & FileAttributes.ReparsePoint) != 0) continue;
                    foreach (var mealDir in Directory.EnumerateDirectories(userDir))
                    {
                        if (!int.TryParse(Path.GetFileName(mealDir), out var mealId) || (File.GetAttributes(mealDir) & FileAttributes.ReparsePoint) != 0) continue;
                        if (!await db.Meals.AnyAsync(x => x.Id == mealId && x.UserId == userId && x.Status != 0, stoppingToken)) Directory.Delete(mealDir, true);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { logger.LogWarning("Photo reconciliation failed; will retry"); }
        }
    }
}
