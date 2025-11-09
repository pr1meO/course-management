using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public interface ITeacherService
{
    Task<TeacherDto> AddAsync(
        string login,
        string passwordHash,
        string firstName,
        string lastName,
        string middleName);

    Task<IEnumerable<TeacherDto>> GetAsync();

    Task<TeacherDto> GetByIdAsync(Guid id);

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

    public async Task<TeacherDto> AddAsync(
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

        TeacherDto teacherDto = new()
        {
            Id = teacher.Id,
            Login = login,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
        };

        return teacherDto;
    }

    public async Task<IEnumerable<TeacherDto>> GetAsync()
    {
        IEnumerable<Teacher> teachers = await _teachersRepository.GetAsync();

        IEnumerable<TeacherDto> teachersDto = teachers
            .Select(t => new TeacherDto
            {
                Id = t.Id,
                Login = t.Login,
                FirstName = t.FirstName,
                LastName = t.LastName,
                MiddleName = t.MiddleName,
            })
            .ToList();

        return teachersDto;
    }

    public async Task<TeacherDto> GetByIdAsync(Guid id)
    {
        Teacher? teacher = await _teachersRepository
            .GetByIdAsync(id)
            ?? throw new InvalidOperationException();

        TeacherDto teacherDto = new()
        {
            Id = teacher.Id,
            Login = teacher.Login,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
        };

        return teacherDto;
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
