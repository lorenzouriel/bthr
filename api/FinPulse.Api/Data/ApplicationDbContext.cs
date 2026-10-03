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
    public DbSet<WeeklyRoutine> WeeklyRoutines { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<PersonalRecord> PersonalRecords { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<WaterIntake> WaterIntakes { get; set; }
    public DbSet<BodyMetric> BodyMetrics { get; set; }
    public DbSet<SleepLog> SleepLogs { get; set; }
    public DbSet<MeditationSession> MeditationSessions { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitLog> HabitLogs { get; set; }
    public DbSet<SubstanceLog> SubstanceLogs { get; set; }
    public DbSet<SymptomLog> SymptomLogs { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User).WithMany(u => u.Habits)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.HabitName }).IsUnique()
                .HasDatabaseName("uq_habits_user_name");
        });

        modelBuilder.Entity<HabitLog>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User).WithMany(u => u.HabitLogs)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.HabitId, e.LogDate }).IsUnique()
                .HasDatabaseName("uq_habit_logs_user_habit_date");
            entity.HasOne(e => e.Habit).WithMany().HasForeignKey(e => e.HabitId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SubstanceLog>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User).WithMany(u => u.SubstanceLogs)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SymptomLog>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User).WithMany(u => u.SymptomLogs)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
         // User configuration
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

        // WeeklyRoutine configuration
        modelBuilder.Entity<WeeklyRoutine>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.WeeklyRoutines)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Workout configuration
        modelBuilder.Entity<Workout>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Workouts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PersonalRecord configuration
        modelBuilder.Entity<PersonalRecord>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PersonalRecords)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Meal configuration
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Meals)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WaterIntake configuration
        modelBuilder.Entity<WaterIntake>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.WaterIntakes)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BodyMetric configuration
        modelBuilder.Entity<BodyMetric>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.BodyMetrics)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SleepLog configuration
        modelBuilder.Entity<SleepLog>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SleepLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MeditationSession configuration
        modelBuilder.Entity<MeditationSession>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.MeditationSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // JournalEntry configuration
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.JournalEntries)
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
