using System.ComponentModel.DataAnnotations;

namespace bthr.Api.DTOs;

public class CreateHabitRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string HabitName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string TargetFrequency { get; set; } = "Daily";

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateHabitRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? HabitName { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [StringLength(20, MinimumLength = 1)]
    public string? TargetFrequency { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, 1)]
    public short? Status { get; set; }
}

public class HabitResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string HabitName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string TargetFrequency { get; set; } = "Daily";
    public string? Description { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
