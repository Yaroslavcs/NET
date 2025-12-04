using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;

namespace LayeredArchitecture.Domain.ValueObjects;

public class Address : ValueObject
{
    [BsonElement("street")]
    public string Street { get; }
    
    [BsonElement("city")]
    public string City { get; }
    
    [BsonElement("country")]
    public string Country { get; }
    
    [BsonElement("zipCode")]
    public string ZipCode { get; }
    
    private Address() { }
    
    public Address(string street, string city, string country, string zipCode)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
        yield return ZipCode;
    }
}