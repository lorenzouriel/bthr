using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("goals")]
public class Goal
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("target_amount", TypeName = "decimal(18,2)")]
    public decimal TargetAmount { get; set; }

    [Required]
    [Column("current_amount", TypeName = "decimal(18,2)")]
    public decimal CurrentAmount { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    [Required]
    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
