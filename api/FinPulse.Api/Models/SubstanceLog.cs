using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("substance_logs", Schema = "body")]
public class SubstanceLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("consumed_at")]
    public DateTime ConsumedAt { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("substance_type")]
    public string SubstanceType { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "numeric(6,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
