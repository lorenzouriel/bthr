using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// Budget Request DTOs
public class CreateBudgetRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public decimal AmountLimit { get; set; }

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class UpdateBudgetRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public decimal? AmountLimit { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

// Budget Response DTO
public class BudgetResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AmountLimit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
