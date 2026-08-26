using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateBodyMetricRequest
{
    [Required]
    public DateTime MeasuredDate { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? BodyFatPercent { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateBodyMetricRequest
{
    public DateTime? MeasuredDate { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? BodyFatPercent { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class BodyMetricResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime MeasuredDate { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? BodyFatPercent { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
