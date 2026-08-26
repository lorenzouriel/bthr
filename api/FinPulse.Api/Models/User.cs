using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(15)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(1024)]
    [Column("password")]
    public string? Password { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public byte Status { get; set; } = 1;

    [Column("plan")]
    public byte Plan { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<Earning> Earnings { get; set; } = new List<Earning>();
    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public virtual ICollection<Investment> Investments { get; set; } = new List<Investment>();
    public virtual ICollection<WeeklyRoutine> WeeklyRoutines { get; set; } = new List<WeeklyRoutine>();
    public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
    public virtual ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
    public virtual ICollection<WaterIntake> WaterIntakes { get; set; } = new List<WaterIntake>();
    public virtual ICollection<BodyMetric> BodyMetrics { get; set; } = new List<BodyMetric>();
    public virtual ICollection<SleepLog> SleepLogs { get; set; } = new List<SleepLog>();
    public virtual ICollection<MeditationSession> MeditationSessions { get; set; } = new List<MeditationSession>();
    public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
