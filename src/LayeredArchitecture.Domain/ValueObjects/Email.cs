using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;

namespace LayeredArchitecture.Domain.ValueObjects;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    [BsonElement("value")]
    public string Value { get; }
    
    private Email() { }
    
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        
        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("Invalid email format", nameof(value));
        
        Value = value.ToLowerInvariant();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
}