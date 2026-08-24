using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

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
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("due_day")]
    public byte DueDay { get; set; }

    [MaxLength(100)]
    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "BRL";

    [Required]
    [Column("is_recurrent")]
    public bool IsRecurrent { get; set; } = true;

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    [Column("recurrence_type")]
    public string? RecurrenceType { get; set; }

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
