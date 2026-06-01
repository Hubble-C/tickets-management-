using System;
using tickets_management.Enums;

namespace tickets_management.Models.ViewModels;

public class BoughtTicketDto
{
    public int UserId { get; set; }
    public string Seat { get; set; }
    public decimal Price { get; set; }
    public DateTime EventDate { get; set; }
    public int EventId { get; set; }
    public int VenueId { get; set; }
    public string TicketCode { get; set; }
    
    // DB raw status
    public string DbStatus { get; set; }

    // Dynamic computed status
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TicketStatus Status
    {
        get
        {
            if (string.Equals(DbStatus, "Scanned", StringComparison.OrdinalIgnoreCase))
                return TicketStatus.Scanned;
            
            if (string.Equals(DbStatus, "Expired", StringComparison.OrdinalIgnoreCase))
                return TicketStatus.Expired;

            return TicketStatus.Pending;
        }
    }
}
