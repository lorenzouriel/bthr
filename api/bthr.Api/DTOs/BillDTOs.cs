using System.ComponentModel.DataAnnotations;

namespace bthr.Api.DTOs;

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

    public DateOnly? DueDate { get; set; }
    public int? RecurrenceInterval { get; set; } = 1;
    public DateOnly? NextDueDate { get; set; }
    public DateOnly? PaidDate { get; set; }

    [MaxLength(50)]
    public string? RecurrenceType { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}

public class UpdateBillRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? PaymentMethod
    {
        get => _paymentMethod;
        set { _paymentMethod = value; HasPaymentMethod = true; }
    }
    private string? _paymentMethod;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasPaymentMethod { get; private set; }

    public decimal? Amount { get; set; }

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    public DateOnly? DueDate
    {
        get => _dueDate;
        set { _dueDate = value; HasDueDate = true; }
    }
    private DateOnly? _dueDate;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasDueDate { get; private set; }
    public int? RecurrenceInterval
    {
        get => _recurrenceInterval;
        set { _recurrenceInterval = value; HasRecurrenceInterval = true; }
    }
    private int? _recurrenceInterval;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasRecurrenceInterval { get; private set; }
    public DateOnly? NextDueDate
    {
        get => _nextDueDate;
        set { _nextDueDate = value; HasNextDueDate = true; }
    }
    private DateOnly? _nextDueDate;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasNextDueDate { get; private set; }
    public DateOnly? PaidDate
    {
        get => _paidDate;
        set { _paidDate = value; HasPaidDate = true; }
    }
    private DateOnly? _paidDate;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasPaidDate { get; private set; }

    [MaxLength(50)]
    public string? RecurrenceType
    {
        get => _recurrenceType;
        set { _recurrenceType = value; HasRecurrenceType = true; }
    }
    private string? _recurrenceType;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasRecurrenceType { get; private set; }

    [MaxLength(255)]
    public string? Description
    {
        get => _description;
        set { _description = value; HasDescription = true; }
    }
    private string? _description;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasDescription { get; private set; }
}

public class BillResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? RecurrenceInterval { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RecurrenceType { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
