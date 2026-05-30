using tickets_management.Models;
using tickets_management.Models.ViewModels;

namespace tickets_management.Services.Interfaces;

public interface IOrderService
{
    public Task<Order?> SaveOrderAsync(TempOrder order);
    public Task<TempOrder?> GetOrderAsync(string orderId);
    public Task AddSeatAsync(string username, SeatViewModel seat);
    public Task RemoveSeatAsync(string username, string seatId);
    public Task ClearOrderAsync(string username);
}