using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Repositories;

public interface ITeachersRepository
{
    Task<bool> ExistsByIdAsync(Guid id);

    Task<bool> ExistsByLoginAsync(string login);

    Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName);

    Task<IEnumerable<Teacher>> GetAsync(
        int offset,
        int limit);

    Task<Teacher?> GetByIdAsync(Guid id);

    Task<Teacher?> GetByLoginAsync(string login);

    Task<int> UpdateByIdAsync(
        Guid id,
        string login,
        string firstName,
        string lastName,
        string middleName);

    Task<int> RemoveByIdAsync(Guid id);
}

public class TeachersRepository : ITeachersRepository
{
    private readonly AppDbContext _appDbContext;

    public TeachersRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        return await _appDbContext.Teachers
            .AnyAsync(b => b.Id == id);
    }

    public async Task<bool> ExistsByLoginAsync(string login)
    {
        return await _appDbContext.Teachers
            .AnyAsync(b => b.Login == login);
    }

    public async Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName)
    {
        Teacher teacher = new()
        {
            Login = login,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
        };

        await _appDbContext.Teachers.AddAsync(teacher);
        await _appDbContext.SaveChangesAsync();

        return teacher;
    }

    public async Task<IEnumerable<Teacher>> GetAsync(
        int offset,
        int limit)
    {
        return await _appDbContext.Teachers
            .AsNoTracking()
            .OrderBy(t => t.LastName)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Teacher?> GetByIdAsync(Guid id)
    {
        return await _appDbContext.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Teacher?> GetByLoginAsync(string login)
    {
        return await _appDbContext.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Login == login);
    }

    public async Task<int> UpdateByIdAsync(
        Guid id,
        string login,
        string firstName,
        string lastName,
        string middleName)
    {
        Teacher? teacher = await _appDbContext.Teachers
            .FindAsync(id)
            ?? throw new InvalidOperationException();

        return await _appDbContext.Teachers
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Login, login)
                .SetProperty(c => c.FirstName, firstName)
                .SetProperty(c => c.LastName, lastName)
                .SetProperty(c => c.MiddleName, middleName));
    }

    public async Task<int> RemoveByIdAsync(Guid id)
    {
        return await _appDbContext.Teachers
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync();
    }
}