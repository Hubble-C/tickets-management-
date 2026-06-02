using Microsoft.AspNetCore.Mvc;
using Dapper;
using tickets_management.Services.Interfaces;

namespace tickets_management.Controllers
{
    public class ValidateRequest
    {
        public string TicketCode { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/tickets")]
    public class TicketsApiController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public TicketsApiController(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest request)
        {
            var code = (request?.TicketCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(code))
                return Ok(new { Result = "NotFound", TicketCode = code, Seat = (string?)null });

            using var conn = _dbConnectionFactory.GetSalesConnection();

            var ticket = await conn.QueryFirstOrDefaultAsync<TicketRow>(
                "SELECT ticket_code AS TicketCode, seat AS Seat, status AS Status FROM tickets WHERE ticket_code = @Code",
                new { Code = code });

            if (ticket == null)
                return Ok(new { Result = "NotFound", TicketCode = code, Seat = (string?)null });

            if (string.Equals(ticket.Status, "Scanned", StringComparison.OrdinalIgnoreCase))
                return Ok(new { Result = "AlreadyUsed", TicketCode = ticket.TicketCode, Seat = ticket.Seat });

            if (string.Equals(ticket.Status, "Expired", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ticket.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return Ok(new { Result = "InvalidState", TicketCode = ticket.TicketCode, Seat = ticket.Seat });

            var rows = await conn.ExecuteAsync(
                "UPDATE tickets SET status = 'Scanned', updated_at = NOW() WHERE ticket_code = @Code AND status <> 'Scanned'",
                new { Code = ticket.TicketCode });

            if (rows == 0)
                return Ok(new { Result = "AlreadyUsed", TicketCode = ticket.TicketCode, Seat = ticket.Seat });

            return Ok(new { Result = "Granted", TicketCode = ticket.TicketCode, Seat = ticket.Seat });
        }

        private class TicketRow
        {
            public string TicketCode { get; set; } = string.Empty;
            public string Seat { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}
