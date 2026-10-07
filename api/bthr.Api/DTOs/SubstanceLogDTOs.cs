using System.ComponentModel.DataAnnotations;

namespace bthr.Api.DTOs;

public class CreateSubstanceLogRequest
{
    [Required]
    public DateTime? ConsumedAt { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string SubstanceType { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0", "9999.99")]
    public decimal? Amount { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SubstanceLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime ConsumedAt { get; set; }
    public string SubstanceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
