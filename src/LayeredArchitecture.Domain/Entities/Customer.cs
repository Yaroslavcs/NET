using MongoDB.Bson.Serialization.Attributes;
using LayeredArchitecture.Domain.Common;
using LayeredArchitecture.Domain.ValueObjects;

namespace LayeredArchitecture.Domain.Entities;

public class Customer : BaseEntity
{
    [BsonElement("firstName")]
    public string FirstName { get; private set; } = string.Empty;
    
    [BsonElement("lastName")]
    public string LastName { get; private set; } = string.Empty;
    
    [BsonElement("email")]
    public string Email { get; private set; } = string.Empty;
    
    [BsonElement("phone")]
    public string? Phone { get; private set; }
    
    [BsonElement("address")]
    public Address? Address { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("loyaltyPoints")]
    public int LoyaltyPoints { get; private set; }
    
    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; private set; }

    // Private constructor for MongoDB deserialization
    private Customer()
    {
    }

    public Customer(string firstName, string lastName, string email, string? phone = null, Address? address = null, DateTime? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLower();
        Phone = phone?.Trim();
        Address = address;
        DateOfBirth = dateOfBirth;
        IsActive = true;
        LoyaltyPoints = 0;
    }

    public void UpdatePersonalInfo(string firstName, string lastName, string email, string? phone = null, DateTime? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLower();
        Phone = phone?.Trim();
        DateOfBirth = dateOfBirth;
        
        UpdateTimestamp();
    }

    public void UpdateAddress(Address? address)
    {
        Address = address;
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

    public void AddLoyaltyPoints(int points)
    {
        if (points < 0)
            throw new ArgumentException("Loyalty points cannot be negative", nameof(points));

        LoyaltyPoints += points;
        UpdateTimestamp();
    }

    public void RedeemLoyaltyPoints(int points)
    {
        if (points < 0)
            throw new ArgumentException("Loyalty points cannot be negative", nameof(points));
        
        if (LoyaltyPoints < points)
            throw new InvalidOperationException("Insufficient loyalty points");

        LoyaltyPoints -= points;
        UpdateTimestamp();
    }

    public string GetFullName()
    {
        return $"{FirstName} {LastName}";
    }

    public int? GetAge()
    {
        if (!DateOfBirth.HasValue)
            return null;

        var today = DateTime.UtcNow;
        var age = today.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}