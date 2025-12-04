using MongoDB.Bson;

namespace LayeredArchitecture.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = ObjectId.GenerateNewId();
        CreatedAt = DateTime.UtcNow;
    }

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}