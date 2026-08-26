using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("workouts", Schema = "body")]
public class Workout
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("workout_date")]
    public DateTime WorkoutDate { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("routine_name")]
    public string RoutineName { get; set; } = string.Empty;

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("calories_burned", TypeName = "decimal(8,2)")]
    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
