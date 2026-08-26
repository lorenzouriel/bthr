using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("body_metrics", Schema = "body")]
public class BodyMetric
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("measured_date")]
    public DateTime MeasuredDate { get; set; }

    [Column("weight_kg", TypeName = "decimal(5,2)")]
    public decimal? WeightKg { get; set; }

    [Column("height_cm", TypeName = "decimal(5,2)")]
    public decimal? HeightCm { get; set; }

    [Column("body_fat_percent", TypeName = "decimal(4,2)")]
    public decimal? BodyFatPercent { get; set; }

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
