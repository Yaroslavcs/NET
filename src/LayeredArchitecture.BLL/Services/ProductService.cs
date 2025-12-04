using AutoMapper;
using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LayeredArchitecture.BLL.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all products");
            throw new BusinessException("An error occurred while retrieving products", ex);
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting product with ID {ProductId}", id);
            throw new BusinessException($"An error occurred while retrieving product with ID {id}", ex);
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto productDto)
    {
        if (productDto == null)
        {
            throw new ValidationException("Product data cannot be null");
        }

        ValidateProductData(productDto);

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var existingProduct = await _unitOfWork.Products.GetByNameAsync(productDto.Name);
            if (existingProduct != null)
            {
                throw new BusinessException($"Product with name '{productDto.Name}' already exists");
            }

            var product = _mapper.Map<Product>(productDto);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Product created successfully with ID {ProductId}", product.Id);
            return _mapper.Map<ProductDto>(product);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while creating product");
            throw new BusinessException("An error occurred while creating product", ex);
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto productDto)
    {
        if (id <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        if (productDto == null)
        {
            throw new ValidationException("Product data cannot be null");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
            if (existingProduct == null)
            {
                throw new NotFoundException("Product", id);
            }

            if (!string.IsNullOrEmpty(productDto.Name) && productDto.Name != existingProduct.Name)
            {
                var productWithName = await _unitOfWork.Products.GetByNameAsync(productDto.Name);
                if (productWithName != null)
                {
                    throw new BusinessException($"Product with name '{productDto.Name}' already exists");
                }
            }

            _mapper.Map(productDto, existingProduct);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Product updated successfully with ID {ProductId}", id);
            return _mapper.Map<ProductDto>(existingProduct);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while updating product with ID {ProductId}", id);
            throw new BusinessException($"An error occurred while updating product with ID {id}", ex);
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                throw new NotFoundException("Product", id);
            }

            if (product.StockQuantity > 0)
            {
                throw new BusinessException("Cannot delete product with existing stock");
            }

            await _unitOfWork.Products.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Product deleted successfully with ID {ProductId}", id);
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
            _logger.LogError(ex, "Error occurred while deleting product with ID {ProductId}", id);
            throw new BusinessException($"An error occurred while deleting product with ID {id}", ex);
        }
    }

    public async Task<bool> ProductExistsAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        try
        {
            return await _unitOfWork.Products.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if product exists with ID {ProductId}", id);
            throw new BusinessException($"An error occurred while checking product existence with ID {id}", ex);
        }
    }

    public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
    {
        if (productId <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        if (quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero");
        }

        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException("Product", productId);
            }

            return product.StockQuantity >= quantity;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking product availability for product ID {ProductId} and quantity {Quantity}", productId, quantity);
            throw new BusinessException($"An error occurred while checking product availability", ex);
        }
    }

    public async Task<bool> UpdateProductStockAsync(int productId, int quantity)
    {
        if (productId <= 0)
        {
            throw new ValidationException("Product ID must be greater than zero");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException("Product", productId);
            }

            var newStockQuantity = product.StockQuantity + quantity;
            if (newStockQuantity < 0)
            {
                throw new BusinessException($"Insufficient stock. Current stock: {product.StockQuantity}, requested change: {quantity}");
            }

            product.StockQuantity = newStockQuantity;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Product stock updated successfully for product ID {ProductId}. New stock: {NewStock}", productId, newStockQuantity);
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
            _logger.LogError(ex, "Error occurred while updating product stock for product ID {ProductId}", productId);
            throw new BusinessException($"An error occurred while updating product stock", ex);
        }
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string? searchTerm, decimal? minPrice, decimal? maxPrice)
    {
        try
        {
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                throw new ValidationException("Minimum price cannot be greater than maximum price");
            }

            var products = await _unitOfWork.Products.SearchAsync(searchTerm, minPrice, maxPrice);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching products with search term '{SearchTerm}', min price {MinPrice}, max price {MaxPrice}", searchTerm, minPrice, maxPrice);
            throw new BusinessException("An error occurred while searching products", ex);
        }
    }

    private void ValidateProductData(CreateProductDto productDto)
    {
        if (string.IsNullOrWhiteSpace(productDto.Name))
        {
            throw new ValidationException("Product name is required");
        }

        if (productDto.Name.Length > 200)
        {
            throw new ValidationException("Product name cannot exceed 200 characters");
        }

        if (productDto.Price < 0)
        {
            throw new ValidationException("Product price cannot be negative");
        }

        if (productDto.Price > 1000000)
        {
            throw new ValidationException("Product price cannot exceed 1,000,000");
        }

        if (productDto.StockQuantity < 0)
        {
            throw new ValidationException("Product stock quantity cannot be negative");
        }

        if (!string.IsNullOrEmpty(productDto.SKU) && productDto.SKU.Length > 50)
        {
            throw new ValidationException("SKU cannot exceed 50 characters");
        }
    }
}