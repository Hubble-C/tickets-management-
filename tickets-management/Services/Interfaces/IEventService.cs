using tickets_management.Models;

namespace tickets_management.Services.Interfaces;

public interface IEventService
{
    Task<IEnumerable<Events>> GetActiveEventsAsync();
}