using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// Investment Request DTOs
public class CreateInvestmentRequest
{
    [Required]
    [MaxLength(50)]
    public string InvestmentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AssetName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Broker { get; set; }

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public decimal InvestedAmount { get; set; }

    public decimal? CurrentValue { get; set; }

    [Required]
    public DateTime PurchaseDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public decimal? AnnualYieldPercent { get; set; }

    public decimal? ProfitLoss { get; set; }
}

public class UpdateInvestmentRequest
{
    [MaxLength(50)]
    public string? InvestmentType { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? AssetName { get; set; }

    [MaxLength(100)]
    public string? Broker { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    public decimal? InvestedAmount { get; set; }

    public decimal? CurrentValue { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public decimal? AnnualYieldPercent { get; set; }

    public decimal? ProfitLoss { get; set; }
}

// Investment Response DTO
public class InvestmentResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string InvestmentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? Broker { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InvestedAmount { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal? AnnualYieldPercent { get; set; }
    public decimal? ProfitLoss { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
