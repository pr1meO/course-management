using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Repositories;

public interface ICoursesRepository
{
    Task<Course> AddAsync(
        string title,
        string description,
        Guid teacherId);

    Task<IEnumerable<Course>> GetAsync(
        int offset,
        int limit);

    Task<Course?> GetByIdAsync(Guid id);

    Task<int> UpdateByIdAsync(
        Guid id,
        Guid teacherId,
        string title,
        string description);

    Task<int> RemoveByIdAsync(Guid id);
}

public class CoursesRepository : ICoursesRepository
{
    private readonly AppDbContext _appDbContext;

    public CoursesRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Course> AddAsync(
        string title,
        string description,
        Guid teacherId)
    {
        Course course = new()
        {
            Title = title,
            Description = description,
            TeacherId = teacherId,
        };

        await _appDbContext.Courses.AddAsync(course);
        await _appDbContext.SaveChangesAsync();

        return course;
    }

    public async Task<IEnumerable<Course>> GetAsync(
        int offset,
        int limit)
    {
        return await _appDbContext.Courses
            .Include(c => c.Teacher)
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await _appDbContext.Courses
            .Include(c => c.Teacher)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> UpdateByIdAsync(
        Guid id,
        Guid teacherId,
        string title,
        string description)
    {
        return await _appDbContext.Courses
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Title, title)
                .SetProperty(c => c.Description, description)
                .SetProperty(c => c.TeacherId, teacherId));
    }

    public async Task<int> RemoveByIdAsync(Guid id)
    {
        return await _appDbContext.Courses
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync();
    }
}