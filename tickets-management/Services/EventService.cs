using Dapper;
using tickets_management.Models;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class EventService : IEventService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public EventService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<Events>> GetActiveEventsAsync()
    {
        using var connection = _dbConnectionFactory.GetCatalogConnection();

        var sql = @"
            SELECT 
                e.Id,
                e.Name,
                e.Description,
                e.StartDate,
                e.EndDate,
                e.VenueId,
                e.IsActive,
                e.PosterUrl,
                e.CompanyId,
                v.Name AS VenueName
            FROM Events e
            LEFT JOIN Venues v ON v.Id = e.VenueId
            WHERE e.IsActive = 1
            ORDER BY e.StartDate ASC";

        return await connection.QueryAsync<Events>(sql);
    }
}