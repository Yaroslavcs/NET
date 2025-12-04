using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.API.Services.Caching;
using LayeredArchitecture.API.Telemetry;
using LayeredArchitecture.Application.Interfaces;
using LayeredArchitecture.Domain.Entities;
using System.Globalization;

namespace LayeredArchitecture.API.Services.GrpcServices;

public class OrderGrpcService : OrderService.OrderServiceBase
{
    private readonly IOrderService _orderService;
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(
        IOrderService orderService,
        IDistributedCacheService cacheService,
        ILogger<OrderGrpcService> logger)
    {
        _orderService = orderService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public override async Task<CachedOrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetOrder");
        try
        {
            var cacheKey = $"order:{request.Id}";
            
            // Try to get from cache first
            var cachedOrder = await _cacheService.GetAsync<OrderResponse>(cacheKey);
            if (cachedOrder != null)
            {
                _logger.LogDebug("Order {OrderId} found in cache", request.Id);
                
                return new CachedOrderResponse
                {
                    Order = cachedOrder,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var order = await _orderService.GetByIdAsync(request.Id);
            if (order == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.Id} not found"));
            }

            var orderResponse = MapToOrderResponse(order);
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, orderResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedOrderResponse
            {
                Order = orderResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedOrdersResponse> GetAllOrders(GetAllOrdersRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetAllOrders");
        try
        {
            var cacheKey = $"orders:page:{request.Page}:size:{request.PageSize}";
            
            // Try to get from cache first
            var cachedOrders = await _cacheService.GetAsync<OrdersResponse>(cacheKey);
            if (cachedOrders != null)
            {
                _logger.LogDebug("Orders page {Page} found in cache", request.Page);
                
                return new CachedOrdersResponse
                {
                    Orders = cachedOrders,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var orders = await _orderService.GetAllAsync(request.Page, request.PageSize);
            var ordersResponse = new OrdersResponse
            {
                Orders = { orders.Select(MapToOrderResponse) },
                TotalCount = orders.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, ordersResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedOrdersResponse
            {
                Orders = ordersResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all orders");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedOrdersResponse> GetOrdersByCustomer(GetOrdersByCustomerRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetOrdersByCustomer");
        try
        {
            var cacheKey = $"orders:customer:{request.CustomerId}:page:{request.Page}:size:{request.PageSize}";
            
            // Try to get from cache first
            var cachedOrders = await _cacheService.GetAsync<OrdersResponse>(cacheKey);
            if (cachedOrders != null)
            {
                _logger.LogDebug("Orders for customer {CustomerId} page {Page} found in cache", request.CustomerId, request.Page);
                
                return new CachedOrdersResponse
                {
                    Orders = cachedOrders,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var orders = await _orderService.GetByCustomerIdAsync(request.CustomerId, request.Page, request.PageSize);
            var ordersResponse = new OrdersResponse
            {
                Orders = { orders.Select(MapToOrderResponse) },
                TotalCount = orders.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, ordersResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedOrdersResponse
            {
                Orders = ordersResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by customer {CustomerId}", request.CustomerId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.CreateOrder");
        try
        {
            var order = new Order
            {
                CustomerId = request.CustomerId,
                Items = request.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Subtotal
                }).ToList(),
                TotalAmount = request.TotalAmount.Amount,
                Currency = request.TotalAmount.Currency
            };

            var createdOrder = await _orderService.CreateAsync(order);
            var response = MapToOrderResponse(createdOrder);

            // Invalidate related caches
            await InvalidateOrderCaches();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<OrderResponse> UpdateOrderStatus(UpdateOrderStatusRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.UpdateOrderStatus");
        try
        {
            var existingOrder = await _orderService.GetByIdAsync(request.Id);
            if (existingOrder == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.Id} not found"));
            }

            existingOrder.Status = MapToOrderStatus(request.Status);

            var updatedOrder = await _orderService.UpdateAsync(existingOrder);
            var response = MapToOrderResponse(updatedOrder);

            // Invalidate related caches
            await InvalidateOrderCaches();
            await _cacheService.RemoveAsync($"order:{request.Id}");

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {OrderId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<DeleteOrderResponse> DeleteOrder(DeleteOrderRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.DeleteOrder");
        try
        {
            var result = await _orderService.DeleteAsync(request.Id);
            if (!result)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.Id} not found"));
            }

            // Invalidate related caches
            await InvalidateOrderCaches();
            await _cacheService.RemoveAsync($"order:{request.Id}");

            return new DeleteOrderResponse
            {
                Success = true,
                Message = $"Order {request.Id} deleted successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedTotalRevenueResponse> GetTotalRevenue(GetTotalRevenueRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetTotalRevenue");
        try
        {
            var cacheKey = $"revenue:{request.StartDate}:{request.EndDate}";
            
            // Try to get from cache first
            var cachedRevenue = await _cacheService.GetAsync<TotalRevenueResponse>(cacheKey);
            if (cachedRevenue != null)
            {
                _logger.LogDebug("Total revenue for period {StartDate}-{EndDate} found in cache", request.StartDate, request.EndDate);
                
                return new CachedTotalRevenueResponse
                {
                    Revenue = cachedRevenue,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 600,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var startDate = DateTime.ParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            
            var totalRevenue = await _orderService.GetTotalRevenueAsync(startDate, endDate);
            var revenueResponse = new TotalRevenueResponse
            {
                TotalRevenue = totalRevenue,
                Currency = "USD",
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, revenueResponse, TimeSpan.FromMinutes(10), TimeSpan.FromHours(1));

            return new CachedTotalRevenueResponse
            {
                Revenue = revenueResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 600,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total revenue for period {StartDate}-{EndDate}", request.StartDate, request.EndDate);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<OrderStatisticsResponse> GetOrderStatistics(GetOrderStatisticsRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetOrderStatistics");
        try
        {
            var cacheKey = $"order_stats:{request.StartDate}:{request.EndDate}";
            
            // Try to get from cache first
            var cachedStats = await _cacheService.GetAsync<OrderStatisticsResponse>(cacheKey);
            if (cachedStats != null)
            {
                _logger.LogDebug("Order statistics for period {StartDate}-{EndDate} found in cache", request.StartDate, request.EndDate);
                
                return cachedStats;
            }

            // If not in cache, get from service
            var startDate = DateTime.ParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            
            var statistics = await _orderService.GetOrderStatisticsAsync(startDate, endDate);
            
            var statsResponse = new OrderStatisticsResponse
            {
                TotalOrders = statistics.TotalOrders,
                PendingOrders = statistics.PendingOrders,
                ConfirmedOrders = statistics.ConfirmedOrders,
                ShippedOrders = statistics.ShippedOrders,
                DeliveredOrders = statistics.DeliveredOrders,
                CancelledOrders = statistics.CancelledOrders,
                TotalRevenue = new MoneyAmount
                {
                    Amount = statistics.TotalRevenue,
                    Currency = "USD"
                },
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 600,
                    CacheSource = "database"
                }
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, statsResponse, TimeSpan.FromMinutes(10), TimeSpan.FromHours(1));

            return statsResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order statistics for period {StartDate}-{EndDate}", request.StartDate, request.EndDate);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = { order.Items.Select(MapToOrderItem) },
            TotalAmount = new MoneyAmount
            {
                Amount = order.TotalAmount,
                Currency = order.Currency
            },
            Status = MapToOrderStatusProto(order.Status),
            OrderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CreatedAt = order.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            UpdatedAt = order.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private OrderItem MapToOrderItem(Domain.Entities.OrderItem item)
    {
        return new OrderItem
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Subtotal = item.Subtotal
        };
    }

    private OrderStatus MapToOrderStatusProto(Domain.Entities.OrderStatus status)
    {
        return status switch
        {
            Domain.Entities.OrderStatus.Pending => OrderStatus.OrderStatusPending,
            Domain.Entities.OrderStatus.Confirmed => OrderStatus.OrderStatusConfirmed,
            Domain.Entities.OrderStatus.Shipped => OrderStatus.OrderStatusShipped,
            Domain.Entities.OrderStatus.Delivered => OrderStatus.OrderStatusDelivered,
            Domain.Entities.OrderStatus.Cancelled => OrderStatus.OrderStatusCancelled,
            _ => OrderStatus.OrderStatusUnspecified
        };
    }

    private Domain.Entities.OrderStatus MapToOrderStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.OrderStatusPending => Domain.Entities.OrderStatus.Pending,
            OrderStatus.OrderStatusConfirmed => Domain.Entities.OrderStatus.Confirmed,
            OrderStatus.OrderStatusShipped => Domain.Entities.OrderStatus.Shipped,
            OrderStatus.OrderStatusDelivered => Domain.Entities.OrderStatus.Delivered,
            OrderStatus.OrderStatusCancelled => Domain.Entities.OrderStatus.Cancelled,
            _ => Domain.Entities.OrderStatus.Pending
        };
    }

    private async Task InvalidateOrderCaches()
    {
        try
        {
            // Remove all order list caches
            await _cacheService.RemoveByPrefixAsync("orders:");
            await _cacheService.RemoveByPrefixAsync("revenue:");
            await _cacheService.RemoveByPrefixAsync("order_stats:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating order caches");
        }
    }
}