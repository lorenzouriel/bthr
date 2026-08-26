using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreatePersonalRecordRequest
{
    [Required]
    [MaxLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string MetricType { get; set; } = string.Empty;

    [Required]
    public decimal Value { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public DateTime AchievedDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PersonalRecordResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime AchievedDate { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
