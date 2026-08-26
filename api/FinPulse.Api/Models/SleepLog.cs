using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("sleep_logs", Schema = "body")]
public class SleepLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("bed_time")]
    public DateTime BedTime { get; set; }

    [Required]
    [Column("wake_time")]
    public DateTime WakeTime { get; set; }

    [Column("total_hours", TypeName = "decimal(4,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalHours { get; set; }

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
