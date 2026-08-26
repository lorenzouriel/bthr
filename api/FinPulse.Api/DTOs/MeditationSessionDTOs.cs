using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateMeditationSessionRequest
{
    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public short DurationMinutes { get; set; }

    [Required]
    [MaxLength(50)]
    public string MeditationType { get; set; } = string.Empty;

    public short? MoodBefore { get; set; }

    public short? MoodAfter { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateMeditationSessionRequest
{
    public DateTime? SessionDate { get; set; }

    public short? DurationMinutes { get; set; }

    [MaxLength(50)]
    public string? MeditationType { get; set; }

    public short? MoodBefore { get; set; }

    public short? MoodAfter { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class MeditationSessionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime SessionDate { get; set; }
    public short DurationMinutes { get; set; }
    public string MeditationType { get; set; } = string.Empty;
    public short? MoodBefore { get; set; }
    public short? MoodAfter { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
