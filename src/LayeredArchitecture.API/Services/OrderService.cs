using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.Application.Common.Interfaces;
using LayeredArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayeredArchitecture.API.Services;

public class OrderService : OrderService.OrderServiceBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<OrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting order with ID: {OrderId}", request.Id);

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Order with ID {request.Id} not found"));
        }

        return new OrderResponse
        {
            Id = order.Id.ToString(),
            CustomerId = order.CustomerId.ToString(),
            OrderDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.OrderDate.ToUniversalTime()),
            Status = MapOrderStatusToProto(order.Status),
            TotalAmount = new MoneyAmount
            {
                Amount = order.TotalAmount,
                Currency = order.Currency
            },
            Items = { MapOrderItemsToProto(order.Items) },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<OrdersResponse> GetAllOrders(GetAllOrdersRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting all orders with page: {Page}, pageSize: {PageSize}, status: {Status}", 
            request.Page, request.PageSize, request.Status);

        var query = _context.Orders.Include(o => o.Items).AsQueryable();

        if (request.Status != OrderStatusFilter.All)
        {
            var domainStatus = MapProtoToOrderStatus(request.Status);
            query = query.Where(o => o.Status == domainStatus);
        }

        if (!string.IsNullOrEmpty(request.CustomerId))
        {
            query = query.Where(o => o.CustomerId == Guid.Parse(request.CustomerId));
        }

        var totalCount = await query.CountAsync(context.CancellationToken);
        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(context.CancellationToken);

        var response = new OrdersResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        response.Orders.AddRange(orders.Select(order => new OrderResponse
        {
            Id = order.Id.ToString(),
            CustomerId = order.CustomerId.ToString(),
            OrderDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.OrderDate.ToUniversalTime()),
            Status = MapOrderStatusToProto(order.Status),
            TotalAmount = new MoneyAmount
            {
                Amount = order.TotalAmount,
                Currency = order.Currency
            },
            Items = { MapOrderItemsToProto(order.Items) },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.UpdatedAt.ToUniversalTime())
        }));

        return response;
    }

    public override async Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating new order for customer: {CustomerId}", request.CustomerId);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.Parse(request.CustomerId),
            OrderDate = request.OrderDate.ToDateTime(),
            Status = Domain.Enums.OrderStatus.Pending,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice),
            Currency = "USD",
            Items = request.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.Parse(item.ProductId),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Order created successfully with ID: {OrderId}", order.Id);

        return new OrderResponse
        {
            Id = order.Id.ToString(),
            CustomerId = order.CustomerId.ToString(),
            OrderDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.OrderDate.ToUniversalTime()),
            Status = MapOrderStatusToProto(order.Status),
            TotalAmount = new MoneyAmount
            {
                Amount = order.TotalAmount,
                Currency = order.Currency
            },
            Items = { MapOrderItemsToProto(order.Items) },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<OrderResponse> UpdateOrderStatus(UpdateOrderStatusRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Updating order status for ID: {OrderId} to {Status}", request.Id, request.Status);

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found for status update", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Order with ID {request.Id} not found"));
        }

        order.Status = MapProtoToOrderStatus(request.Status);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Order status updated successfully for ID: {OrderId}", order.Id);

        return new OrderResponse
        {
            Id = order.Id.ToString(),
            CustomerId = order.CustomerId.ToString(),
            OrderDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.OrderDate.ToUniversalTime()),
            Status = MapOrderStatusToProto(order.Status),
            TotalAmount = new MoneyAmount
            {
                Amount = order.TotalAmount,
                Currency = order.Currency
            },
            Items = { MapOrderItemsToProto(order.Items) },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(order.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<DeleteOrderResponse> DeleteOrder(DeleteOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Deleting order with ID: {OrderId}", request.Id);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID: {OrderId} not found for deletion", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Order with ID {request.Id} not found"));
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Order deleted successfully with ID: {OrderId}", order.Id);

        return new DeleteOrderResponse { Success = true };
    }

    private OrderStatus MapOrderStatusToProto(Domain.Enums.OrderStatus status)
    {
        return status switch
        {
            Domain.Enums.OrderStatus.Pending => OrderStatus.Pending,
            Domain.Enums.OrderStatus.Processing => OrderStatus.Processing,
            Domain.Enums.OrderStatus.Shipped => OrderStatus.Shipped,
            Domain.Enums.OrderStatus.Delivered => OrderStatus.Delivered,
            Domain.Enums.OrderStatus.Cancelled => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };
    }

    private Domain.Enums.OrderStatus MapProtoToOrderStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => Domain.Enums.OrderStatus.Pending,
            OrderStatus.Processing => Domain.Enums.OrderStatus.Processing,
            OrderStatus.Shipped => Domain.Enums.OrderStatus.Shipped,
            OrderStatus.Delivered => Domain.Enums.OrderStatus.Delivered,
            OrderStatus.Cancelled => Domain.Enums.OrderStatus.Cancelled,
            _ => Domain.Enums.OrderStatus.Pending
        };
    }

    private Domain.Enums.OrderStatus MapProtoToOrderStatus(OrderStatusFilter status)
    {
        return status switch
        {
            OrderStatusFilter.Pending => Domain.Enums.OrderStatus.Pending,
            OrderStatusFilter.Processing => Domain.Enums.OrderStatus.Processing,
            OrderStatusFilter.Shipped => Domain.Enums.OrderStatus.Shipped,
            OrderStatusFilter.Delivered => Domain.Enums.OrderStatus.Delivered,
            OrderStatusFilter.Cancelled => Domain.Enums.OrderStatus.Cancelled,
            _ => Domain.Enums.OrderStatus.Pending
        };
    }

    private IEnumerable<OrderItemMessage> MapOrderItemsToProto(List<OrderItem> items)
    {
        return items.Select(item => new OrderItemMessage
        {
            Id = item.Id.ToString(),
            ProductId = item.ProductId.ToString(),
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        });
    }
}