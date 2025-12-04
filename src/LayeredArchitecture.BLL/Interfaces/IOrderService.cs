using LayeredArchitecture.BLL.DTOs;

namespace LayeredArchitecture.BLL.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto);
    Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderAsync(int id);
    Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(int customerId);
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status);
    Task<decimal> CalculateOrderTotalAsync(int orderId);
    Task<bool> ValidateOrderItemsAsync(IEnumerable<CreateOrderItemDto> items);
}