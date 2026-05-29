using System.Collections.Concurrent;
using tickets_management.Models;
using tickets_management.Models.ViewModels;
using tickets_management.Services.Interfaces;
using tickets_management.Data;
using tickets_management.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace tickets_management.Services;


public class OrderService : IOrderService
{
    private readonly ConcurrentDictionary<string, TempOrder> _orders = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task AddSeatAsync(string username, SeatViewModel seatVm)
    {
        var order = _orders.GetOrAdd(username, _ => new TempOrder()
        {
            SelectedSeats = new List<Seat>(),
            Subtotal = 0,
            Total = 0
        });

        
        var parts = seatVm.Id?.Split('-');
        var rowChar = (parts != null && parts.Length > 0 && parts[0].Length == 1)
            ? parts[0][0]
            : seatVm.Row?.Length > 0 ? seatVm.Row[0] : 'A';

        var label = seatVm.Zone?.ToUpper() == "VIP" ? LabelZone.VIP : LabelZone.GENERAL;

        lock (order.SelectedSeats)
        {
            if (!order.SelectedSeats.Any(s => s.Row == rowChar && s.SeatNumber == seatVm.Num))
            {
                order.SelectedSeats.Add(new Seat
                {
                    Row        = rowChar,
                    SeatNumber = seatVm.Num,
                    Zone      = label,
                    Price      = seatVm.Price
                });
            }

            order.Subtotal = order.SelectedSeats.Sum(s => s.Price);
            order.Tax      = Math.Round(order.Subtotal * 0.21m, 2);
            order.Total    = order.Subtotal + order.Tax - order.Discount;
        }

        return Task.CompletedTask;
    }

    public Task RemoveSeatAsync(string username, string seatId)
    {
        if (_orders.TryGetValue(username, out var order))
        {
            var parts = seatId?.Split('-');
            if (parts != null && parts.Length == 2
                && parts[0].Length == 1
                && int.TryParse(parts[1], out var num))
            {
                var rowChar = parts[0][0];
                lock (order.SelectedSeats)
                {
                    order.SelectedSeats.RemoveAll(s => s.Row == rowChar && s.SeatNumber == num);
                    order.Subtotal = order.SelectedSeats.Sum(s => s.Price);
                    order.Tax      = Math.Round(order.Subtotal * 0.21m, 2);
                    order.Total    = order.Subtotal + order.Tax - order.Discount;
                }
            }
        }

        return Task.CompletedTask;
    }

    public async Task SaveOrderAsync(TempOrder tempOrder)
    {
        var found = _orders.TryGetValue(tempOrder.OrderId, out _);

        var orderEntity = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status    = OrderStatus.Pending,
            Items     = new List<OrderItem>(),
        };

        foreach (var seat in tempOrder.SelectedSeats)
        {
            orderEntity.Items.Add(new OrderItem
            {
                Quantity = 1,
                Fee      = tempOrder.ServiceRate,
                SubTotal = tempOrder.Subtotal,
                Total    = tempOrder.Total,
                EventId  = int.Parse(tempOrder.EventId),
            });
        }

        if (found)
        {
           
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MySqlDbContext>();
            await context.Orders.AddAsync(orderEntity);
            await context.SaveChangesAsync();
            _orders.TryRemove(tempOrder.OrderId, out _);
        }
    }

    public Task<TempOrder?> GetOrderAsync(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult<TempOrder?>(order);
    }

    public Task ClearOrderAsync(string orderId)
    {
        _orders.TryRemove(orderId, out _);
        return Task.CompletedTask;
    }
}