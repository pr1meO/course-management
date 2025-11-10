using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public interface ITeacherService
{
    Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName);

    Task<IEnumerable<Teacher>> GetAsync();

    Task<Teacher> GetByIdAsync(Guid id);

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

    // private readonly IPasswordHasher _hasher;
    public TeacherService(ITeachersRepository teachersRepository)
    {
        _teachersRepository = teachersRepository;
    }

    public async Task<Teacher> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName)
    {
        // IPasswordHasher
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
            ?? throw new InvalidOperationException();

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
            throw new InvalidOperationException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _teachersRepository.RemoveByIdAsync(id);

        if (result == 0)
            throw new InvalidOperationException();
    }
}
