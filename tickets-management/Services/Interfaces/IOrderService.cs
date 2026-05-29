using tickets_management.Models;

namespace tickets_management.Services.Interfaces;

public interface IOrderService
{
    public Task SaveOrderAsync(TempOrder order);
    public Task<TempOrder?> GetOrderAsync(string orderId);
    public Task AddSeatAsync(string username, string seatId);
    public Task RemoveSeatAsync(string username, string seatId);
    public Task ClearOrderAsync(string username);
}