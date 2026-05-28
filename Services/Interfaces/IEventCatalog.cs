using tickets_management.Dto;

namespace tickets_management.Services.Interfaces;

public interface IEventCatalog
{
    /// <summary>
    /// Reads event details (name, date, venue) for the given ids from the catalog,
    /// keyed by event id. Ids with no matching event are simply absent.
    /// </summary>
    Task<IReadOnlyDictionary<int, EventInfoDto>> GetEventsAsync(
        IEnumerable<int> eventIds, CancellationToken ct = default);
}
