using System.Collections.Concurrent;
using tickets_management.Models;
using tickets_management.Models.ViewModels;
using tickets_management.Services.Interfaces;
using System.Collections.Concurrent;
using Azure.Identity;
using tickets_management.Data;
using tickets_management.Enums;

namespace tickets_management.Services;

public class OrderService : IOrderService
{
    private readonly ConcurrentDictionary<string, TempOrder> _orders = new();
    private readonly MySqlDbContext _context;

    public OrderService(MySqlDbContext context)
    {
        _context = context;
    }
    
    public  Task AddSeatAsync(string username, string seatId)
    {
        int convertedNumber;
        
        var order = _orders.GetOrAdd(username, _ => new TempOrder()
        {
            SelectedSeats = new List<Seat>(),
            Subtotal = 0,
            Total = 0
            
        });
        
        if (int.TryParse(seatId, out convertedNumber))
        {
            lock (order.SelectedSeats)
            {
                if (!order.SelectedSeats.Any(s => s.SeatNumber == convertedNumber))
                {
                    var newSeat = new Seat
                    {
                        SeatNumber = convertedNumber
                    };
                    order.SelectedSeats.Add(newSeat);
                }
            }
            
        }
        
        return Task.CompletedTask;
    }

    public  Task RemoveSeatAsync(string username, string seatId)
    {
        int convertedNumber;
        
        var order = _orders.GetOrAdd(username, _ => new TempOrder
        {
            SelectedSeats = new List<Seat>()
        });
        
        if (_orders.TryGetValue(username, out order) && int.TryParse(seatId, out convertedNumber))
        {
            lock (order.SelectedSeats)
            {
                order.SelectedSeats.RemoveAll(s => s.SeatNumber == convertedNumber);
            }
        }
        
        return Task.CompletedTask;
    }

    public async Task SaveOrderAsync(TempOrder tempOrder)
    {
        var orderValue = _orders.TryGetValue(tempOrder.OrderId, out var order );

        var OrderEntity = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>(),
        };
        
        foreach (var seat in tempOrder.SelectedSeats)
        {
            var item = new OrderItem
            {
                Quantity = 1,
                Fee = tempOrder.ServiceRate,
                SubTotal = tempOrder.Subtotal,
                Total = tempOrder.Total,
                EventId = int.Parse(tempOrder.EventId),
            };
            OrderEntity.Items.Add(item);
        }
        
        if (orderValue == true)
        {
            await _context.Orders.AddAsync(OrderEntity);
            await _context.SaveChangesAsync();
            _orders.TryRemove(tempOrder.OrderId, out _);
        }
    }

    public Task<TempOrder?> GetOrderAsync(string orderId)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult<TempOrder?>(order);
        }
        return Task.FromResult<TempOrder?>(null);
    }

    public Task ClearOrderAsync(string orderId)
    {
        _orders.TryRemove(orderId, out _);
        return Task.CompletedTask;
    }
}
