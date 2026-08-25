using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FinPulse.Api.Models;

namespace FinPulse.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Earning> Earnings { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<Investment> Investments { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        // Expense configuration
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Expenses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Earning configuration
        modelBuilder.Entity<Earning>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Earnings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Bill configuration
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Bills)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Budget configuration
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Budgets)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Goal configuration
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Goals)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Investment configuration
        modelBuilder.Entity<Investment>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Investments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcNullableDateTimeConverter() : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
