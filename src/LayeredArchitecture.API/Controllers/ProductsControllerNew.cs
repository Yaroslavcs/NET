using MediatR;
using Microsoft.AspNetCore.Mvc;
using LayeredArchitecture.Application.Products.Commands.CreateProduct;
using LayeredArchitecture.Application.Products.Commands.UpdateProduct;
using LayeredArchitecture.Application.Products.Commands.DeleteProduct;
using LayeredArchitecture.Application.Products.Queries.GetProductById;
using LayeredArchitecture.Application.Products.Queries.GetProductsList;

namespace LayeredArchitecture.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ISender mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProductsListVm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductsListVm>> GetAllProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = new GetProductsListQuery { PageNumber = pageNumber, PageSize = pageSize };
            var products = await _mediator.Send(query);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all products");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving products",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> GetProductById(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID is required",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var query = new GetProductByIdQuery { Id = id };
            var product = await _mediator.Send(query);
            
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting product with ID {ProductId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving product",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="command">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> CreateProduct([FromBody] CreateProductCommand command)
    {
        try
        {
            if (command == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var productId = await _mediator.Send(command);
            
            return CreatedAtAction(
                nameof(GetProductById),
                new { id = productId },
                productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating product");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating product",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="command">Product update data</param>
    /// <returns>Updated product ID</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> UpdateProduct(string id, [FromBody] UpdateProductCommand command)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID is required",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (command == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            command.Id = id;
            await _mediator.Send(command);
            
            return Ok(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating product with ID {ProductId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while updating product",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID is required",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var command = new DeleteProductCommand { Id = id };
            await _mediator.Send(command);
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while deleting product with ID {ProductId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting product",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}