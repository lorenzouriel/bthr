using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// Expense Request DTOs
public class CreateExpenseRequest
{
    [Required]
    [MaxLength(255)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }
}

public class UpdateExpenseRequest
{
    [MaxLength(255)]
    public string? Category { get; set; }

    [MaxLength(255)]
    public string? PaymentMethod { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public DateTime? ExpenseDate { get; set; }
}

// Expense Response DTO
public class ExpenseResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime ExpenseDate { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
