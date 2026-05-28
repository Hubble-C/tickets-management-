using System.Data;
using Dapper;
using tickets_management.Dto;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

/// <summary>Reads read-only event data from the catalog DB via Dapper.</summary>
public class EventCatalog : IEventCatalog
{
    private readonly IDbConnection _catalogDb;

    public EventCatalog(IDbConnection catalogDb) => _catalogDb = catalogDb;

    public async Task<IReadOnlyDictionary<int, EventInfoDto>> GetEventsAsync(
        IEnumerable<int> eventIds, CancellationToken ct = default)
    {
        var ids = eventIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<int, EventInfoDto>();

        var rows = await _catalogDb.QueryAsync<EventInfoDto>(new CommandDefinition(
            """
            SELECT e.Id, e.Name, e.StartDate, v.Name AS VenueName
            FROM Events e
            LEFT JOIN Venues v ON v.Id = e.VenueId
            WHERE e.Id IN @Ids
            """,
            new { Ids = ids },
            cancellationToken: ct));

        return rows.ToDictionary(e => e.Id);
    }
}
