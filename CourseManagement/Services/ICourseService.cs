using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public interface ICourseService
{
    Task<Course> AddAsync(
        string title,
        string description,
        Guid teacherId);

    Task<IEnumerable<Course>> GetAsync();

    Task<Course> GetByIdAsync(Guid id);

    Task UpdateByIdAsync(
        Guid id,
        Guid teacherId,
        string title,
        string description);

    Task RemoveByIdAsync(Guid id);
}

public class CourseService : ICourseService
{
    private readonly ICoursesRepository _coursesRepository;
    private readonly ITeachersRepository _teachersRepository;

    public CourseService(
        ICoursesRepository coursesRepository,
        ITeachersRepository teachersRepository)
    {
        _coursesRepository = coursesRepository;
        _teachersRepository = teachersRepository;
    }

    public async Task<Course> AddAsync(
        string title,
        string description,
        Guid teacherId)
    {
        bool exists = await _teachersRepository.ExistsByIdAsync(teacherId);

        if (!exists)
            throw new KeyNotFoundException();

        Course course = await _coursesRepository.AddAsync(title, description, teacherId);

        return course;
    }

    public async Task<IEnumerable<Course>> GetAsync()
    {
        IEnumerable<Course> courses = await _coursesRepository.GetAsync();

        return courses;
    }

    public async Task<Course> GetByIdAsync(Guid id)
    {
        Course? course = await _coursesRepository
            .GetByIdAsync(id)
            ?? throw new KeyNotFoundException();

        return course;
    }

    public async Task UpdateByIdAsync(
        Guid id,
        Guid teacherId,
        string title,
        string description)
    {
        bool exists = await _teachersRepository.ExistsByIdAsync(teacherId);

        if (!exists)
            throw new KeyNotFoundException();

        int result = await _coursesRepository.UpdateByIdAsync(
            id,
            teacherId,
            title,
            description);

        if (result == 0)
            throw new KeyNotFoundException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _coursesRepository.RemoveByIdAsync(id);

        if (result == 0)
            throw new KeyNotFoundException();
    }
}