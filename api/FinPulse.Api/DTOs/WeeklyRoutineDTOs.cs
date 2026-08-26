using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateWeeklyRoutineRequest
{
    [Required]
    public short DayOfWeek { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoutineName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateWeeklyRoutineRequest
{
    public short? DayOfWeek { get; set; }

    [MaxLength(100)]
    public string? RoutineName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public short? Status { get; set; }
}

public class WeeklyRoutineResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public short DayOfWeek { get; set; }
    public string RoutineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
