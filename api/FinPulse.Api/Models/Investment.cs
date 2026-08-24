using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("investments")]
public class Investment
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
    [Column("asset_name")]
    public string AssetName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("investment_type")]
    public string InvestmentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Column("invested_amount", TypeName = "decimal(18,2)")]
    public decimal InvestedAmount { get; set; }

    [Column("current_value", TypeName = "decimal(18,2)")]
    public decimal? CurrentValue { get; set; }

    [Column("profit_loss", TypeName = "decimal(18,2)")]
    public decimal? ProfitLoss { get; set; }

    [Column("annual_yield_percent", TypeName = "decimal(8,4)")]
    public decimal? AnnualYieldPercent { get; set; }

    [MaxLength(255)]
    [Column("broker")]
    public string? Broker { get; set; }

    [Required]
    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("maturity_date")]
    public DateTime? MaturityDate { get; set; }

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
