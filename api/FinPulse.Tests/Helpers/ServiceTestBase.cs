using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;

namespace FinPulse.Tests.Helpers;

/// <summary>
/// Base class for service unit tests that provides an in-memory database context.
/// Each test gets an isolated database instance to ensure test independence.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected ApplicationDbContext Context { get; }

    protected ServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
