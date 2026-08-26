using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("personal_records", Schema = "body")]
public class PersonalRecord
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("exercise_name")]
    public string ExerciseName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("metric_type")]
    public string MetricType { get; set; } = string.Empty;

    [Required]
    [Column("value", TypeName = "decimal(10,2)")]
    public decimal Value { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [Required]
    [Column("achieved_date")]
    public DateTime AchievedDate { get; set; }

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
