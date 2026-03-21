using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
