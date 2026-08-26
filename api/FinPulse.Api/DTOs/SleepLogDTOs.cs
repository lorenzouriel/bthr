using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateSleepLogRequest
{
    [Required]
    public DateTime BedTime { get; set; }

    [Required]
    public DateTime WakeTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateSleepLogRequest
{
    public DateTime? BedTime { get; set; }

    public DateTime? WakeTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class SleepLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime BedTime { get; set; }
    public DateTime WakeTime { get; set; }
    public decimal TotalHours { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
