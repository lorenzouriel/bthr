using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bthr.Api.Models;

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
    public short Status { get; set; } = 1;

    [Column("telegram_user_id")]
    public long? TelegramUserId { get; set; }

    // Navigation properties
    public virtual ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public virtual ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
    public virtual ICollection<SubstanceLog> SubstanceLogs { get; set; } = new List<SubstanceLog>();
    public virtual ICollection<SymptomLog> SymptomLogs { get; set; } = new List<SymptomLog>();
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
