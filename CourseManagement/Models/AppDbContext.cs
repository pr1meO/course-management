using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Models;

public class AppDbContext : DbContext
{
    public DbSet<Teacher> Teachers { get; set; }

    public DbSet<Course> Courses { get; set; }

    public AppDbContext(DbContextOptions options)
        : base(options)
    {
    }
}
