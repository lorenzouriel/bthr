using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bthr.Api.Models;

[Table("bills")]
public class Bill
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
    [Column("bill_name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("due_date")]
    public DateOnly? DueDate { get; set; }

    [MaxLength(100)]
    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "BRL";

    [Column("recurrence_interval")]
    public int? RecurrenceInterval { get; set; } = 1;

    [Column("next_due_date")]
    public DateOnly? NextDueDate { get; set; }

    [Column("paid_date")]
    public DateOnly? PaidDate { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(50)]
    [Column("recurrence_type")]
    public string? RecurrenceType { get; set; }

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
