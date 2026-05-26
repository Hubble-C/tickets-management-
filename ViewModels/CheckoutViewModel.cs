using System.ComponentModel.DataAnnotations;
using tickets_management.Enums;

namespace tickets_management.ViewModels;

/// <summary>Box-office purchase form: one event, one or more seats.</summary>
public class CheckoutViewModel
{
    [Required, Display(Name = "Customer Id")]
    [Range(1, int.MaxValue, ErrorMessage = "A valid customer is required.")]
    public int CustomerId { get; set; }

    [Required, StringLength(50), Display(Name = "NIT / Document")]
    public string Nit { get; set; } = string.Empty;

    [Required, Display(Name = "Ticket type Id")]
    [Range(1, int.MaxValue, ErrorMessage = "A valid ticket type is required.")]
    public int TicketTypeId { get; set; }

    [Required, Display(Name = "Seats (comma-separated)")]
    public string Seats { get; set; } = string.Empty;

    [DataType(DataType.Time), Display(Name = "Show time")]
    public TimeOnly HourAt { get; set; }

    [Display(Name = "Payment method")]
    public TypePayment PaymentMethod { get; set; }

    /// <summary>Distinct, trimmed seat labels parsed from the comma-separated input.</summary>
    public List<string> ParseSeats() =>
        Seats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList();
}
