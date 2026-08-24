using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // Expense configuration
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Expenses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Earning configuration
        modelBuilder.Entity<Earning>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Earnings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Bill configuration
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Bills)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Budget configuration
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Budgets)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Goal configuration
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Goals)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Investment configuration
        modelBuilder.Entity<Investment>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Investments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
