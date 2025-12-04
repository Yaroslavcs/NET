using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;
using LayeredArchitecture.Domain.ValueObjects;

namespace LayeredArchitecture.Domain.Entities;

public class Product : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;
    
    [BsonElement("description")]
    public string Description { get; private set; } = string.Empty;
    
    [BsonElement("price")]
    public Money Price { get; private set; } = new Money(0, "USD");
    
    [BsonElement("stockQuantity")]
    public int StockQuantity { get; private set; }
    
    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty;
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("sku")]
    public string Sku { get; private set; } = string.Empty;
    
    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    // Private constructor for MongoDB deserialization
    private Product()
    {
    }

    public Product(string name, string description, Money price, int stockQuantity, string category, string sku)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Product category cannot be empty", nameof(category));
        
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Product SKU cannot be empty", nameof(sku));
        
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        Category = category.Trim();
        Sku = sku.Trim().ToUpper();
        IsActive = true;
    }

    public void UpdateDetails(string name, string description, Money price, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Product category cannot be empty", nameof(category));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Category = category.Trim();
        
        UpdateTimestamp();
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        var trimmedTag = tag.Trim().ToLower();
        if (!Tags.Contains(trimmedTag))
        {
            Tags.Add(trimmedTag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        var trimmedTag = tag.Trim().ToLower();
        if (Tags.Remove(trimmedTag))
        {
            UpdateTimestamp();
        }
    }

    public void ClearTags()
    {
        if (Tags.Any())
        {
            Tags.Clear();
            UpdateTimestamp();
        }
    }
}