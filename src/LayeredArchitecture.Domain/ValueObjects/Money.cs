using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;

namespace LayeredArchitecture.Domain.ValueObjects;

public class Money : ValueObject
{
    [BsonElement("amount")]
    public decimal Amount { get; }
    
    [BsonElement("currency")]
    public string Currency { get; }
    
    private Money() { }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter code", nameof(currency));
        
        Amount = amount;
        Currency = currency.ToUpper();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}