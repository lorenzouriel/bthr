using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateWorkoutRequest
{
    [Required]
    public DateTime WorkoutDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoutineName { get; set; } = string.Empty;

    public int? DurationMinutes { get; set; }

    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateWorkoutRequest
{
    public DateTime? WorkoutDate { get; set; }

    [MaxLength(100)]
    public string? RoutineName { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class WorkoutResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime WorkoutDate { get; set; }
    public string RoutineName { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public decimal? CaloriesBurned { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
