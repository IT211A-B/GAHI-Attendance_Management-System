using Attendance_Management_System.Backend.Interfaces.Repositories;
using Attendance_Management_System.Backend.Persistence;

namespace Attendance_Management_System.Backend.Repositories;

// Generic repository implementation for basic CRUD operations
// Provides a reusable data access pattern for any entity type
public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _dbContext;

    public GenericRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Adds a new entity to the database
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    // Marks an entity as modified for the next save operation
    public void Update(T entity)
    {
        _dbContext.Set<T>().Update(entity);
    }

    // Marks an entity for deletion from the database
    public void Remove(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
    }
}
