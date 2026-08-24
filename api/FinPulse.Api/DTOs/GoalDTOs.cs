using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// Goal Request DTOs
public class CreateGoalRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public decimal TargetAmount { get; set; }

    [Required]
    public decimal CurrentAmount { get; set; }

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; }
}

public class UpdateGoalRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public decimal? TargetAmount { get; set; }

    public decimal? CurrentAmount { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    public DateTime? DueDate { get; set; }

    public short? Status { get; set; }
}

// Goal Response DTO
public class GoalResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
