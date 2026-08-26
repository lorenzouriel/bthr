using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateMealRequest
{
    [Required]
    public DateTime MealDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string MealType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public decimal Calories { get; set; }

    public decimal? ProteinGrams { get; set; }

    public decimal? CarbsGrams { get; set; }

    public decimal? FatGrams { get; set; }
}

public class UpdateMealRequest
{
    public DateTime? MealDate { get; set; }

    [MaxLength(50)]
    public string? MealType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal? Calories { get; set; }

    public decimal? ProteinGrams { get; set; }

    public decimal? CarbsGrams { get; set; }

    public decimal? FatGrams { get; set; }

    public short? Status { get; set; }
}

public class MealResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime MealDate { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Calories { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
