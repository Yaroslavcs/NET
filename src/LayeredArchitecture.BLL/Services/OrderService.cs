using AutoMapper;
using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LayeredArchitecture.BLL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        try
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all orders");
            throw new BusinessException("An error occurred while retrieving orders", ex);
        }
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Order ID must be greater than zero");
        }

        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            return order == null ? null : _mapper.Map<OrderDto>(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting order with ID {OrderId}", id);
            throw new BusinessException($"An error occurred while retrieving order with ID {id}", ex);
        }
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto)
    {
        if (orderDto == null)
        {
            throw new ValidationException("Order data cannot be null");
        }

        ValidateOrderData(orderDto);

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Validate customer exists
            var customer = await _unitOfWork.Customers.GetByIdAsync(orderDto.CustomerId);
            if (customer == null)
            {
                throw new NotFoundException("Customer", orderDto.CustomerId);
            }

            // Validate order items and calculate total
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemDto in orderDto.OrderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);
                if (product == null)
                {
                    throw new NotFoundException("Product", itemDto.ProductId);
                }

                if (product.StockQuantity < itemDto.Quantity)
                {
                    throw new BusinessException($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {itemDto.Quantity}");
                }

                if (itemDto.UnitPrice != product.Price)
                {
                    throw new BusinessException($"Product price mismatch for '{product.Name}'. Expected: {product.Price}, Provided: {itemDto.UnitPrice}");
                }

                var orderItem = _mapper.Map<OrderItem>(itemDto);
                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;

                // Update product stock
                product.StockQuantity -= itemDto.Quantity;
                await _unitOfWork.Products.UpdateAsync(product);
            }

            if (orderDto.OrderItems.Count == 0)
            {
                throw new ValidationException("Order must contain at least one item");
            }

            var order = _mapper.Map<Order>(orderDto);
            order.TotalAmount = totalAmount;
            order.OrderItems = orderItems;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order created successfully with ID {OrderId} for customer {CustomerId}", order.Id, order.CustomerId);
            return _mapper.Map<OrderDto>(order);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while creating order");
            throw new BusinessException("An error occurred while creating order", ex);
        }
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        if (id <= 0)
        {
            throw new ValidationException("Order ID must be greater than zero");
        }

        if (statusDto == null)
        {
            throw new ValidationException("Status data cannot be null");
        }

        if (string.IsNullOrWhiteSpace(statusDto.Status))
        {
            throw new ValidationException("Status is required");
        }

        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" };
        if (!validStatuses.Contains(statusDto.Status))
        {
            throw new ValidationException($"Invalid order status. Valid statuses are: {string.Join(", ", validStatuses)}");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order", id);
            }

            // Validate status transition
            ValidateStatusTransition(order.Status, statusDto.Status);

            order.Status = statusDto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order status updated successfully for order ID {OrderId}. New status: {NewStatus}", id, statusDto.Status);
            return _mapper.Map<OrderDto>(order);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while updating order status for order ID {OrderId}", id);
            throw new BusinessException($"An error occurred while updating order status for order ID {id}", ex);
        }
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Order ID must be greater than zero");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order", id);
            }

            // Only allow deletion of pending orders
            if (order.Status != "Pending")
            {
                throw new BusinessException($"Cannot delete order with status '{order.Status}'. Only pending orders can be deleted.");
            }

            // Restore product stock
            foreach (var item in order.OrderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    await _unitOfWork.Products.UpdateAsync(product);
                }
            }

            await _unitOfWork.Orders.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order deleted successfully with ID {OrderId}", id);
            return true;
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while deleting order with ID {OrderId}", id);
            throw new BusinessException($"An error occurred while deleting order with ID {id}", ex);
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(int customerId)
    {
        if (customerId <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        try
        {
            var orders = await _unitOfWork.Orders.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting orders for customer ID {CustomerId}", customerId);
            throw new BusinessException($"An error occurred while retrieving orders for customer ID {customerId}", ex);
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ValidationException("Status is required");
        }

        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" };
        if (!validStatuses.Contains(status))
        {
            throw new ValidationException($"Invalid order status. Valid statuses are: {string.Join(", ", validStatuses)}");
        }

        try
        {
            var orders = await _unitOfWork.Orders.GetByStatusAsync(status);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting orders with status '{Status}'", status);
            throw new BusinessException($"An error occurred while retrieving orders with status '{status}'", ex);
        }
    }

    public async Task<decimal> CalculateOrderTotalAsync(int orderId)
    {
        if (orderId <= 0)
        {
            throw new ValidationException("Order ID must be greater than zero");
        }

        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException("Order", orderId);
            }

            return order.TotalAmount;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calculating order total for order ID {OrderId}", orderId);
            throw new BusinessException($"An error occurred while calculating order total for order ID {orderId}", ex);
        }
    }

    public async Task<bool> ValidateOrderItemsAsync(IEnumerable<CreateOrderItemDto> items)
    {
        if (items == null || !items.Any())
        {
            throw new ValidationException("Order items cannot be null or empty");
        }

        try
        {
            foreach (var item in items)
            {
                if (item.ProductId <= 0)
                {
                    throw new ValidationException("Product ID must be greater than zero");
                }

                if (item.Quantity <= 0)
                {
                    throw new ValidationException("Quantity must be greater than zero");
                }

                if (item.UnitPrice < 0)
                {
                    throw new ValidationException("Unit price cannot be negative");
                }

                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    throw new NotFoundException("Product", item.ProductId);
                }

                if (product.StockQuantity < item.Quantity)
                {
                    throw new BusinessException($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }

                if (item.UnitPrice != product.Price)
                {
                    throw new BusinessException($"Product price mismatch for '{product.Name}'. Expected: {product.Price}, Provided: {item.UnitPrice}");
                }
            }

            return true;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating order items");
            throw new BusinessException("An error occurred while validating order items", ex);
        }
    }

    private void ValidateOrderData(CreateOrderDto orderDto)
    {
        if (orderDto.CustomerId <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
        {
            throw new ValidationException("Order must contain at least one item");
        }

        if (string.IsNullOrWhiteSpace(orderDto.PaymentMethod))
        {
            throw new ValidationException("Payment method is required");
        }

        var validPaymentMethods = new[] { "CreditCard", "DebitCard", "PayPal", "BankTransfer", "Cash" };
        if (!validPaymentMethods.Contains(orderDto.PaymentMethod))
        {
            throw new ValidationException($"Invalid payment method. Valid methods are: {string.Join(", ", validPaymentMethods)}");
        }

        // Validate each order item
        foreach (var item in orderDto.OrderItems)
        {
            if (item.ProductId <= 0)
            {
                throw new ValidationException("Product ID must be greater than zero");
            }

            if (item.Quantity <= 0)
            {
                throw new ValidationException("Quantity must be greater than zero");
            }

            if (item.UnitPrice < 0)
            {
                throw new ValidationException("Unit price cannot be negative");
            }
        }
    }

    private void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var allowedTransitions = new Dictionary<string, string[]>
        {
            { "Pending", new[] { "Processing", "Cancelled" } },
            { "Processing", new[] { "Shipped", "Cancelled" } },
            { "Shipped", new[] { "Delivered" } },
            { "Delivered", new[] { "Refunded" } },
            { "Cancelled", Array.Empty<string>() },
            { "Refunded", Array.Empty<string>() }
        };

        if (!allowedTransitions.ContainsKey(currentStatus) || 
            !allowedTransitions[currentStatus].Contains(newStatus))
        {
            throw new BusinessException($"Invalid status transition from '{currentStatus}' to '{newStatus}'");
        }
    }
}