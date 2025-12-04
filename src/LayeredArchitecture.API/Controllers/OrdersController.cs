using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LayeredArchitecture.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    /// <returns>List of all orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting all orders");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting all orders");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving orders",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting order with ID {OrderId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting order with ID {OrderId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving order",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="orderDto">Order creation data</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto orderDto)
    {
        try
        {
            if (orderDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var createdOrder = await _orderService.CreateOrderAsync(orderDto);
            
            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = createdOrder.Id },
                createdOrder);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("Insufficient stock"))
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("price mismatch"))
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while creating order");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating order");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating order",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="statusDto">Status update data</param>
    /// <returns>Updated order</returns>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (statusDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Status data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, statusDto);
            if (updatedOrder == null)
            {
                return NotFound();
            }

            return Ok(updatedOrder);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("Invalid status transition"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while updating order status for order ID {OrderId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating order status for order ID {OrderId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while updating order status",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await _orderService.DeleteOrderAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("Only pending orders"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while deleting order with ID {OrderId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while deleting order with ID {OrderId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting order",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get orders by customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <returns>List of orders for the customer</returns>
    [HttpGet("customer/{customerId:int}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(int customerId)
    {
        try
        {
            if (customerId <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var orders = await _orderService.GetOrdersByCustomerAsync(customerId);
            return Ok(orders);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting orders for customer ID {CustomerId}", customerId);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting orders for customer ID {CustomerId}", customerId);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving orders for customer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    /// <param name="status">Order status</param>
    /// <returns>List of orders with the specified status</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByStatus(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Status is required",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting orders with status '{Status}'", status);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting orders with status '{Status}'", status);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving orders by status",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Calculate order total
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order total amount</returns>
    [HttpGet("{id:int}/total")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<decimal>> CalculateOrderTotal(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var total = await _orderService.CalculateOrderTotalAsync(id);
            return Ok(total);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while calculating order total for order ID {OrderId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calculating order total for order ID {OrderId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while calculating order total",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Validate order items
    /// </summary>
    /// <param name="items">Order items to validate</param>
    /// <returns>True if all items are valid, false otherwise</returns>
    [HttpPost("validate-items")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> ValidateOrderItems([FromBody] IEnumerable<CreateOrderItemDto> items)
    {
        try
        {
            if (items == null || !items.Any())
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Order items cannot be null or empty",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var isValid = await _orderService.ValidateOrderItemsAsync(items);
            return Ok(isValid);
        }
        catch (ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("Insufficient stock"))
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (BusinessException ex) when (ex.Message.Contains("price mismatch"))
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while validating order items");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while validating order items");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while validating order items",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}