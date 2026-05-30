using Dapper;
using tickets_management.Data;
using tickets_management.Models;
using tickets_management.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using tickets_management.Enums;

namespace tickets_management.Services;

public class EventService : IEventService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly MySqlDbContext _context;

    public EventService(IDbConnectionFactory dbConnectionFactory,  MySqlDbContext context)
    {
        _context = context;
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


    public async Task<IEnumerable<TicketType>> GetTicketTypesByEventAsync(int eventId)
    {
        using var connection = _dbConnectionFactory.GetCatalogConnection();

        var sql = @"
            SELECT Id, Name, Price, Quantity, EventId
            FROM TicketTypes
            WHERE EventId = @EventId";

        return await connection.QueryAsync<TicketType>(sql, new { EventId = eventId });
    }


    public async Task<IEnumerable<string>> GetOccupiedSeatsByEventAsync(int eventId)
    {
        using var connection = _dbConnectionFactory.GetSalesConnection();
        var sql = @"SELECT seat FROM tickets WHERE event_id = @EventId AND status = 'Active'";
        return await connection.QueryAsync<string>(sql, new { EventId = eventId });
    }
}