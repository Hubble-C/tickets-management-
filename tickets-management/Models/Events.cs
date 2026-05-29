using System;

namespace tickets_management.Models
{
    public class Events
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int VenueId { get; set; }
        public bool IsActive { get; set; }
        public string PosterUrl { get; set; }
        public int? CompanyId { get; set; }


        public string VenueName { get; set; }
        
        public int TicketTypeId { get; set; }
    }
}