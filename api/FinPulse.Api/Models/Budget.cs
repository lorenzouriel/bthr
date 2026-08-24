using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("budgets")]
public class Budget
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
    [Column("amount_limit", TypeName = "decimal(18,2)")]
    public decimal AmountLimit { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    [Required]
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
