using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateHabitLogRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int? HabitId { get; set; }

    [Required]
    public DateOnly? LogDate { get; set; }

    public bool IsCompleted { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateHabitLogRequest
{
    [Range(1, int.MaxValue)]
    public int? HabitId { get; set; }

    public DateOnly? LogDate { get; set; }

    public bool? IsCompleted { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Range(0, 1)]
    public short? Status { get; set; }
}

public class HabitLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HabitId { get; set; }
    public DateOnly LogDate { get; set; }
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
