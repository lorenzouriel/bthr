using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateWaterIntakeRequest
{
    [Required]
    public DateTime IntakeDate { get; set; }

    [Required]
    public int AmountMl { get; set; }
}

public class UpdateWaterIntakeRequest
{
    public DateTime? IntakeDate { get; set; }

    public int? AmountMl { get; set; }

    public short? Status { get; set; }
}

public class WaterIntakeResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime IntakeDate { get; set; }
    public int AmountMl { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
