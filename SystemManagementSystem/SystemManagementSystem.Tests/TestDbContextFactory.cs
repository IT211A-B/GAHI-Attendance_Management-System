using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;

namespace SystemManagementSystem.Tests;

/// <summary>
/// Provides reusable InMemory ApplicationDbContext instances for unit tests.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
