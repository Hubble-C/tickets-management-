using tickets_management.Enums;

namespace tickets_management.Models;

public class Seat
{
    public char Row { get; set; }
    public int SeatNumber { get; set; }
    public LabelZone Label { get; set; }
    public decimal Price { get; set; }
}