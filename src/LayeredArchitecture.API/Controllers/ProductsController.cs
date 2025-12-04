using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LayeredArchitecture.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <returns>List of all products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while getting all products");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting all products");
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
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
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
            _logger.LogError(ex, "Error occurred while getting product with ID {ProductId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
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
    /// <param name="productDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto productDto)
    {
        try
        {
            if (productDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var createdProduct = await _productService.CreateProductAsync(productDto);
            
            return CreatedAtAction(
                nameof(GetProductById),
                new { id = createdProduct.Id },
                createdProduct);
        }
        catch (ValidationException ex)
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
            _logger.LogError(ex, "Error occurred while creating product");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
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
    /// <param name="productDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto productDto)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (productDto == null)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product data cannot be null",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var updatedProduct = await _productService.UpdateProductAsync(id, productDto);
            if (updatedProduct == null)
            {
                return NotFound();
            }

            return Ok(updatedProduct);
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
        catch (BusinessException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while updating product with ID {ProductId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
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
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await _productService.DeleteProductAsync(id);
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
        catch (BusinessException ex) when (ex.Message.Contains("existing stock"))
        {
            return Conflict(ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Error occurred while deleting product with ID {ProductId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
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

    /// <summary>
    /// Search products
    /// </summary>
    /// <param name="searchTerm">Search term (name, description)</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <returns>List of matching products</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice)
    {
        try
        {
            var products = await _productService.SearchProductsAsync(searchTerm, minPrice, maxPrice);
            return Ok(products);
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
            _logger.LogError(ex, "Error occurred while searching products");
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching products");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while searching products",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Check if product exists
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>True if product exists, false otherwise</returns>
    [HttpHead("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProductExists(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var exists = await _productService.ProductExistsAsync(id);
            return exists ? Ok() : NotFound();
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
            _logger.LogError(ex, "Error occurred while checking if product exists with ID {ProductId}", id);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking if product exists with ID {ProductId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while checking product existence",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Check product availability
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="quantity">Quantity to check</param>
    /// <returns>True if product is available, false otherwise</returns>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> CheckProductAvailability(int id, [FromQuery] int quantity)
    {
        try
        {
            if (id <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Product ID must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (quantity <= 0)
            {
                return Problem(
                    title: "Validation Error",
                    detail: "Quantity must be greater than zero",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var isAvailable = await _productService.IsProductAvailableAsync(id, quantity);
            return Ok(isAvailable);
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
            _logger.LogError(ex, "Error occurred while checking product availability for product ID {ProductId} and quantity {Quantity}", id, quantity);
            return Problem(
                title: "Business Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking product availability for product ID {ProductId} and quantity {Quantity}", id, quantity);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while checking product availability",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}