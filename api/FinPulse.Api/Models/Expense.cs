using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("expenses")]
public class Expense
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("expense_date")]
    public DateTime ExpenseDate { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
