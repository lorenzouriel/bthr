using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("meditation_sessions", Schema = "mind")]
public class MeditationSession
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("session_date")]
    public DateTime SessionDate { get; set; }

    [Required]
    [Column("duration_minutes")]
    public short DurationMinutes { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("meditation_type")]
    public string MeditationType { get; set; } = string.Empty;

    [Column("mood_before")]
    public short? MoodBefore { get; set; }

    [Column("mood_after")]
    public short? MoodAfter { get; set; }

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
