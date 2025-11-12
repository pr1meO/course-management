using System.Security.Authentication;
using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public interface ITeacherService
{
    Task ExistsByLoginAsync(string login);

    Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName);

    Task<IEnumerable<Teacher>> GetAsync();

    Task<Teacher> GetByIdAsync(Guid id);

    Task<Teacher> GetByLoginAsync(string login);

    Task UpdateByIdAsync(
        Guid id,
        string login,
        string firstName,
        string lastName,
        string middleName);

    Task RemoveByIdAsync(Guid id);
}

public class TeacherService : ITeacherService
{
    private readonly ITeachersRepository _teachersRepository;

    public TeacherService(ITeachersRepository teachersRepository)
    {
        _teachersRepository = teachersRepository;
    }

    public async Task ExistsByLoginAsync(string login)
    {
        bool exists = await _teachersRepository.ExistsByLoginAsync(login);

        if (exists)
            throw new InvalidOperationException();
    }

    public async Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName)
    {
        Teacher teacher = await _teachersRepository.AddAsync(
            login,
            passwordHash,
            firstName,
            lastName,
            middleName);

        return teacher;
    }

    public async Task<IEnumerable<Teacher>> GetAsync()
    {
        IEnumerable<Teacher> teachers = await _teachersRepository.GetAsync();

        return teachers;
    }

    public async Task<Teacher> GetByIdAsync(Guid id)
    {
        Teacher? teacher = await _teachersRepository
            .GetByIdAsync(id)
            ?? throw new KeyNotFoundException();

        return teacher;
    }

    public async Task<Teacher> GetByLoginAsync(string login)
    {
        Teacher? teacher = await _teachersRepository
            .GetByLoginAsync(login)
            ?? throw new AuthenticationException();

        return teacher;
    }

    public async Task UpdateByIdAsync(
        Guid id,
        string login,
        string firstName,
        string lastName,
        string middleName)
    {
        int result = await _teachersRepository.UpdateByIdAsync(
            id,
            login,
            firstName,
            lastName,
            middleName);

        if (result == 0)
            throw new KeyNotFoundException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _teachersRepository.RemoveByIdAsync(id);

        if (result == 0)
            throw new KeyNotFoundException();
    }
}
