using tickets_management.Dto;
using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse<Order>> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task<ServiceResponse<Order>> MarkAsPaidAsync(int orderId, CancellationToken ct = default);
    Task<ServiceResponse<Order>> CancelAsync(int orderId, CancellationToken ct = default);
    Task<ServiceResponse<Order>> RejectAsync(int orderId, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<SalesReportDto> GetSalesReportAsync(CancellationToken ct = default);
}
