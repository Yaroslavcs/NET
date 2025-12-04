using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.API.Services.Caching;
using LayeredArchitecture.API.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static LayeredArchitecture.API.Grpc.CustomerService;
using static LayeredArchitecture.API.Grpc.OrderService;
using static LayeredArchitecture.API.Grpc.ProductService;

namespace LayeredArchitecture.API.Services;

public class AggregatorService : AggregatorService.AggregatorServiceBase
{
    private readonly ILogger<AggregatorService> _logger;
    private readonly ICacheService _cacheService;
    private readonly ProductServiceClient _productClient;
    private readonly CustomerServiceClient _customerClient;
    private readonly OrderServiceClient _orderClient;

    public AggregatorService(
        ILogger<AggregatorService> logger,
        ICacheService cacheService,
        ProductServiceClient productClient,
        CustomerServiceClient customerClient,
        OrderServiceClient orderClient)
    {
        _logger = logger;
        _cacheService = cacheService;
        _productClient = productClient;
        _customerClient = customerClient;
        _orderClient = orderClient;
    }

    public override async Task<OrderDetailsResponse> GetOrderDetails(GetOrderDetailsRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("GetOrderDetails");
        
        try
        {
            _logger.LogInformation("Getting order details for order ID: {OrderId}", request.OrderId);
            
            // Try to get from cache first
            var cacheKey = $"order_details_{request.OrderId}";
            var cachedResult = await _cacheService.GetAsync<OrderDetailsResponse>(cacheKey);
            
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for order details: {OrderId}", request.OrderId);
                return cachedResult;
            }
            
            _logger.LogDebug("Cache miss for order details: {OrderId}", request.OrderId);
            
            // Get order details
            var orderResponse = await _orderClient.GetOrderAsync(new GetOrderRequest { Id = request.OrderId });
            
            // Get customer details
            var customerResponse = await _customerClient.GetCustomerAsync(new GetCustomerRequest { Id = orderResponse.CustomerId });
            
            // Get product details for each order item
            var orderItems = new List<ProductOrderItem>();
            decimal totalAmount = 0;
            
            foreach (var item in orderResponse.Items)
            {
                var productResponse = await _productClient.GetProductAsync(new GetProductRequest { Id = item.ProductId });
                
                var orderItem = new ProductOrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = productResponse.Name,
                    Quantity = item.Quantity,
                    UnitPrice = (decimal)item.UnitPrice,
                    Subtotal = item.Quantity * (decimal)item.UnitPrice
                };
                
                orderItems.Add(orderItem);
                totalAmount += orderItem.Subtotal;
            }
            
            // Convert MoneyAmount to decimal
            var orderTotalAmount = (decimal)orderResponse.TotalAmount.Amount;
            
            var result = new OrderDetailsResponse
            {
                Order = orderResponse,
                Customer = customerResponse,
                Items = { orderItems },
                TotalAmount = totalAmount,
                Status = orderResponse.Status.ToString()
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            
            activity?.SetTag("order.id", request.OrderId);
            activity?.SetTag("customer.id", orderResponse.CustomerId);
            activity?.SetTag("items.count", orderItems.Count);
            activity?.SetTag("total.amount", (double)totalAmount);
            
            return result;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while getting order details for order ID: {OrderId}", request.OrderId);
            activity?.RecordException(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting order details for order ID: {OrderId}", request.OrderId);
            activity?.RecordException(ex);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CustomerWithOrdersResponse> GetCustomerWithOrders(GetCustomerWithOrdersRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("GetCustomerWithOrders");
        
        try
        {
            _logger.LogInformation("Getting customer with orders for customer ID: {CustomerId}", request.CustomerId);
            
            // Try to get from cache first
            var cacheKey = $"customer_orders_{request.CustomerId}";
            var cachedResult = await _cacheService.GetAsync<CustomerWithOrdersResponse>(cacheKey);
            
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for customer with orders: {CustomerId}", request.CustomerId);
                return cachedResult;
            }
            
            _logger.LogDebug("Cache miss for customer with orders: {CustomerId}", request.CustomerId);
            
            // Get customer details
            var customerResponse = await _customerClient.GetCustomerAsync(new GetCustomerRequest { Id = request.CustomerId });
            
            // Get customer's orders
            var ordersResponse = await _orderClient.GetOrdersByCustomerAsync(new GetOrdersByCustomerRequest { CustomerId = request.CustomerId });
            
            decimal totalSpent = 0;
            foreach (var order in ordersResponse.Orders)
            {
                totalSpent += (decimal)order.TotalAmount.Amount;
            }
            
            var result = new CustomerWithOrdersResponse
            {
                Customer = customerResponse,
                Orders = { ordersResponse.Orders },
                TotalOrders = ordersResponse.Orders.Count,
                TotalSpent = totalSpent
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            
            activity?.SetTag("customer.id", request.CustomerId);
            activity?.SetTag("orders.count", ordersResponse.Orders.Count);
            activity?.SetTag("total.spent", (double)totalSpent);
            
            return result;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while getting customer with orders for customer ID: {CustomerId}", request.CustomerId);
            activity?.RecordException(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting customer with orders for customer ID: {CustomerId}", request.CustomerId);
            activity?.RecordException(ex);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<ProductWithOrdersResponse> GetProductWithOrders(GetProductWithOrdersRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("GetProductWithOrders");
        
        try
        {
            _logger.LogInformation("Getting product with orders for product ID: {ProductId}", request.ProductId);
            
            // Try to get from cache first
            var cacheKey = $"product_orders_{request.ProductId}";
            var cachedResult = await _cacheService.GetAsync<ProductWithOrdersResponse>(cacheKey);
            
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for product with orders: {ProductId}", request.ProductId);
                return cachedResult;
            }
            
            _logger.LogDebug("Cache miss for product with orders: {ProductId}", request.ProductId);
            
            // Get product details
            var productResponse = await _productClient.GetProductAsync(new GetProductRequest { Id = request.ProductId });
            
            // Get orders containing this product - we need to get all orders and filter them
            var allOrdersResponse = await _orderClient.GetAllOrdersAsync(new GetAllOrdersRequest { Page = 1, PageSize = 100 });
            var ordersWithProduct = new List<OrderResponse>();
            
            int totalQuantitySold = 0;
            decimal totalRevenue = 0;
            
            foreach (var order in allOrdersResponse.Orders)
            {
                bool orderContainsProduct = false;
                foreach (var item in order.Items)
                {
                    if (item.ProductId == request.ProductId)
                    {
                        if (!orderContainsProduct)
                        {
                            ordersWithProduct.Add(order);
                            orderContainsProduct = true;
                        }
                        totalQuantitySold += item.Quantity;
                        totalRevenue += item.Quantity * (decimal)item.UnitPrice;
                    }
                }
            }
            
            var result = new ProductWithOrdersResponse
            {
                Product = productResponse,
                Orders = { ordersWithProduct },
                TotalOrders = ordersWithProduct.Count,
                TotalQuantitySold = totalQuantitySold,
                TotalRevenue = totalRevenue
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            
            activity?.SetTag("product.id", request.ProductId);
            activity?.SetTag("orders.count", ordersWithProduct.Count);
            activity?.SetTag("quantity.sold", totalQuantitySold);
            activity?.SetTag("total.revenue", (double)totalRevenue);
            
            return result;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while getting product with orders for product ID: {ProductId}", request.ProductId);
            activity?.RecordException(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting product with orders for product ID: {ProductId}", request.ProductId);
            activity?.RecordException(ex);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CreateOrderWithValidationResponse> CreateOrderWithValidation(CreateOrderWithValidationRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("CreateOrderWithValidation");
        
        try
        {
            _logger.LogInformation("Creating order with validation for customer ID: {CustomerId}", request.CustomerId);
            
            var validationErrors = new List<ValidationError>();
            decimal totalAmount = 0;
            
            // Validate customer exists
            try
            {
                var customerResponse = await _customerClient.GetCustomerAsync(new GetCustomerRequest { Id = request.CustomerId });
                if (customerResponse == null)
                {
                    validationErrors.Add(new ValidationError { Field = "customer_id", Message = "Customer not found" });
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                validationErrors.Add(new ValidationError { Field = "customer_id", Message = "Customer not found" });
            }
            
            // Validate products and calculate total
            foreach (var item in request.Items)
            {
                try
                {
                    var productResponse = await _productClient.GetProductAsync(new GetProductRequest { Id = item.ProductId });
                    
                    if (productResponse.StockQuantity < item.Quantity)
                    {
                        validationErrors.Add(new ValidationError 
                        { 
                            Field = $"items[{item.ProductId}].quantity", 
                            Message = $"Insufficient stock. Available: {productResponse.StockQuantity}, Requested: {item.Quantity}" 
                        });
                    }
                    
                    totalAmount += item.Quantity * (decimal)item.UnitPrice;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    validationErrors.Add(new ValidationError { Field = $"items[{item.ProductId}].product_id", Message = "Product not found" });
                }
            }
            
            // Validate payment method
            var validPaymentMethods = new[] { "credit_card", "debit_card", "paypal", "bank_transfer" };
            if (!validPaymentMethods.Contains(request.PaymentMethod.ToLower()))
            {
                validationErrors.Add(new ValidationError { Field = "payment_method", Message = "Invalid payment method" });
            }
            
            // Validate shipping address
            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            {
                validationErrors.Add(new ValidationError { Field = "shipping_address", Message = "Shipping address is required" });
            }
            
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Order validation failed for customer ID: {CustomerId}. Errors: {ErrorCount}", 
                    request.CustomerId, validationErrors.Count);
                
                activity?.SetTag("validation.failed", true);
                activity?.SetTag("validation.errors.count", validationErrors.Count);
                
                return new CreateOrderWithValidationResponse
                {
                    Success = false,
                    Message = "Validation failed",
                    ValidationErrors = { validationErrors }
                };
            }
            
            // Create the order
            var createOrderRequest = new CreateOrderRequest
            {
                CustomerId = request.CustomerId
            };
            
            // Convert OrderItemRequest to OrderItem
            foreach (var item in request.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = "", // Will be populated by the order service
                    Quantity = item.Quantity,
                    UnitPrice = (double)item.UnitPrice,
                    Subtotal = (double)(item.Quantity * (decimal)item.UnitPrice)
                };
                createOrderRequest.Items.Add(orderItem);
            }
            
            // Set total amount
            createOrderRequest.TotalAmount = new MoneyAmount
            {
                Amount = (double)totalAmount,
                Currency = "USD" // Default currency
            };
            
            var orderResponse = await _orderClient.CreateOrderAsync(createOrderRequest);
            
            // Invalidate related caches
            await _cacheService.RemoveAsync($"customer_orders_{request.CustomerId}");
            foreach (var item in request.Items)
            {
                await _cacheService.RemoveAsync($"product_orders_{item.ProductId}");
            }
            
            activity?.SetTag("order.created", true);
            activity?.SetTag("order.id", orderResponse.Id);
            activity?.SetTag("order.total_amount", (double)totalAmount);
            
            return new CreateOrderWithValidationResponse
            {
                Success = true,
                OrderId = orderResponse.Id,
                Message = "Order created successfully",
                TotalAmount = (double)totalAmount
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while creating order with validation for customer ID: {CustomerId}", request.CustomerId);
            activity?.RecordException(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating order with validation for customer ID: {CustomerId}", request.CustomerId);
            activity?.RecordException(ex);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }
}