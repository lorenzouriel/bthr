using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateBillRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PaymentMethod { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = "BRL";

    [Required]
    [Range(1, 31)]
    public byte DueDay { get; set; }

    public bool IsRecurrent { get; set; } = true;

    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    public string? RecurrenceType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateBillRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? PaymentMethod { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    [Range(1, 31)]
    public byte? DueDay { get; set; }

    public bool? IsRecurrent { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    public string? RecurrenceType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class BillResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public byte DueDay { get; set; }
    public string? PaymentMethod { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsRecurrent { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RecurrenceType { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
    // Computed fields
    public string DueDate { get; set; } = string.Empty;
    public bool PaidThisMonth { get; set; }
    public string? PaidDate { get; set; }
}
