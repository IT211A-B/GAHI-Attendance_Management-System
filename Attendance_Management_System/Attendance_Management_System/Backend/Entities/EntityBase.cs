// Base class for all entities in the system
// Provides common properties that all entities share
namespace Attendance_Management_System.Backend.Entities;

// Abstract base class - cannot be instantiated directly
public abstract class EntityBase
{
    // Unique identifier for each entity
    public int Id { get; set; }

    // Timestamp when the entity was created, defaults to current UTC time
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
