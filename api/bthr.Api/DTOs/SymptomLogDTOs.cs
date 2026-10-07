using System.ComponentModel.DataAnnotations;

namespace bthr.Api.DTOs;

public class CreateSymptomLogRequest
{
    [Required]
    public DateOnly? LogDate { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Symptom { get; set; } = string.Empty;

    [Range(1, 5)]
    public short? Severity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SymptomLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public string Symptom { get; set; } = string.Empty;
    public short? Severity { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
