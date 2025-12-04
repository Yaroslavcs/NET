using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;
using LayeredArchitecture.Domain.ValueObjects;

namespace LayeredArchitecture.Domain.Entities;

public class Order : BaseEntity
{
    [BsonElement("orderNumber")]
    public string OrderNumber { get; private set; } = string.Empty;
    
    [BsonElement("customerId")]
    public string CustomerId { get; private set; } = string.Empty;
    
    [BsonElement("orderDate")]
    public DateTime OrderDate { get; private set; }
    
    [BsonElement("status")]
    public OrderStatus Status { get; private set; }
    
    [BsonElement("items")]
    public List<OrderItem> Items { get; private set; } = new();
    
    [BsonElement("subtotal")]
    public Money Subtotal { get; private set; } = new Money(0, "USD");
    
    [BsonElement("tax")]
    public Money Tax { get; private set; } = new Money(0, "USD");
    
    [BsonElement("shippingCost")]
    public Money ShippingCost { get; private set; } = new Money(0, "USD");
    
    [BsonElement("totalAmount")]
    public Money TotalAmount { get; private set; } = new Money(0, "USD");
    
    [BsonElement("shippingAddress")]
    public Address? ShippingAddress { get; private set; }
    
    [BsonElement("billingAddress")]
    public Address? BillingAddress { get; private set; }
    
    [BsonElement("notes")]
    public string? Notes { get; private set; }

    // Private constructor for MongoDB deserialization
    private Order()
    {
    }

    public Order(string customerId, Address? shippingAddress = null, Address? billingAddress = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

        OrderNumber = GenerateOrderNumber();
        CustomerId = customerId;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress ?? shippingAddress;
        Notes = notes?.Trim();
        
        CalculateTotals();
    }

    public void AddItem(string productId, string productName, Money unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify items for order that is not in Pending status");

        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = new OrderItem(productId, productName, unitPrice, quantity);
            Items.Add(item);
        }

        CalculateTotals();
        UpdateTimestamp();
    }

    public void RemoveItem(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify items for order that is not in Pending status");

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
            CalculateTotals();
            UpdateTimestamp();
        }
    }

    public void UpdateItemQuantity(string productId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify items for order that is not in Pending status");

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.UpdateQuantity(newQuantity);
            CalculateTotals();
            UpdateTimestamp();
        }
    }

    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be confirmed");
        
        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm order without items");

        Status = OrderStatus.Confirmed;
        UpdateTimestamp();
    }

    public void ShipOrder()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
        UpdateTimestamp();
    }

    public void DeliverOrder()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be delivered");

        Status = OrderStatus.Delivered;
        UpdateTimestamp();
    }

    public void CancelOrder()
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Delivered orders cannot be cancelled");

        Status = OrderStatus.Cancelled;
        UpdateTimestamp();
    }

    public void UpdateShippingAddress(Address address)
    {
        ShippingAddress = address ?? throw new ArgumentNullException(nameof(address));
        UpdateTimestamp();
    }

    public void UpdateBillingAddress(Address? address)
    {
        BillingAddress = address;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdateTimestamp();
    }

    private void CalculateTotals()
    {
        Subtotal = new Money(Items.Sum(i => i.TotalPrice.Amount), "USD");
        Tax = new Money(Subtotal.Amount * 0.08m, "USD"); // 8% tax rate
        TotalAmount = new Money(Subtotal.Amount + Tax.Amount + ShippingCost.Amount, "USD");
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{timestamp}-{random}";
    }
}

public enum OrderStatus
{
    [BsonElement("pending")]
    Pending,
    
    [BsonElement("confirmed")]
    Confirmed,
    
    [BsonElement("shipped")]
    Shipped,
    
    [BsonElement("delivered")]
    Delivered,
    
    [BsonElement("cancelled")]
    Cancelled
}

[BsonIgnoreExtraElements]
public class OrderItem
{
    [BsonElement("productId")]
    public string ProductId { get; private set; } = string.Empty;
    
    [BsonElement("productName")]
    public string ProductName { get; private set; } = string.Empty;
    
    [BsonElement("unitPrice")]
    public Money UnitPrice { get; private set; } = new Money(0, "USD");
    
    [BsonElement("quantity")]
    public int Quantity { get; private set; }
    
    [BsonElement("totalPrice")]
    public Money TotalPrice { get; private set; } = new Money(0, "USD");

    // Private constructor for MongoDB deserialization
    private OrderItem()
    {
    }

    public OrderItem(string productId, string productName, Money unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        Quantity = quantity;
        TotalPrice = new Money(unitPrice.Amount * quantity, unitPrice.Currency);
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        Quantity = newQuantity;
        TotalPrice = new Money(UnitPrice.Amount * newQuantity, UnitPrice.Currency);
    }
}