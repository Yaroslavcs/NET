using System.ComponentModel.DataAnnotations;
using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LayeredArchitecture.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    /// <returns>List of all customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting all customers");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting all customers");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving customers",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }
        catch (BLL.Exceptions.ValidationException ex)
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
            _logger.LogError(ex, "Error occurred while getting customer with ID {CustomerId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting customer with ID {CustomerId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving customer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="customerDto">Customer creation data</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto customerDto)
    {
        try
        {
            if (customerDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var createdCustomer = await _customerService.CreateCustomerAsync(customerDto);
            
            return CreatedAtAction(
                nameof(GetCustomerById),
                new { id = createdCustomer.Id },
                createdCustomer);
        }
        catch (BLL.Exceptions.ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (BusinessException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while creating customer");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating customer");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating customer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="customerDto">Customer update data</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto customerDto)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (customerDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var updatedCustomer = await _customerService.UpdateCustomerAsync(id, customerDto);
            if (updatedCustomer == null)
            {
                return NotFound();
            }

            return Ok(updatedCustomer);
        }
        catch (BLL.Exceptions.ValidationException ex)
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
        catch (BusinessException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while updating customer with ID {CustomerId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating customer with ID {CustomerId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while updating customer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await _customerService.DeleteCustomerAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (BLL.Exceptions.ValidationException ex)
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
        catch (BusinessException ex) when (ex.Message.Contains("existing orders"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while deleting customer with ID {CustomerId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while deleting customer with ID {CustomerId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting customer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Search customers
    /// </summary>
    /// <param name="searchTerm">Search term (name, email, etc.)</param>
    /// <returns>List of matching customers</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> SearchCustomers([FromQuery] string? searchTerm)
    {
        try
        {
            var customers = await _customerService.SearchCustomersAsync(searchTerm);
            return Ok(customers);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while searching customers with term '{SearchTerm}'", searchTerm);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching customers with term '{SearchTerm}'", searchTerm);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while searching customers",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Check if customer exists
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>True if customer exists, false otherwise</returns>
    [HttpHead("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CustomerExists(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Customer ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var exists = await _customerService.CustomerExistsAsync(id);
            return exists ? Ok() : NotFound();
        }
        catch (BLL.Exceptions.ValidationException ex)
        {
            return Problem(
                title: "Validation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while checking if customer exists with ID {CustomerId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking if customer exists with ID {CustomerId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while checking customer existence",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}