using CourseManagement.Contracts.Courses;
using CourseManagement.Models;
using CourseManagement.Repositories;

namespace CourseManagement.Services;

public interface ICourseService
{
    Task<CourseDto> AddAsync(
        string title,
        string description,
        Guid teacherId);

    Task<IEnumerable<CourseDto>> GetAsync();

    Task<CourseDto> GetByIdAsync(Guid id);

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

    public async Task<CourseDto> AddAsync(
        string title,
        string description,
        Guid teacherId)
    {
        bool exists = await _teachersRepository.ExistsByIdAsync(teacherId);

        if (!exists)
            throw new InvalidOperationException();

        Course course = await _coursesRepository.AddAsync(title, description, teacherId);

        CourseDto courseDto = new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
        };

        return courseDto;
    }

    public async Task<IEnumerable<CourseDto>> GetAsync()
    {
        IEnumerable<Course> courses = await _coursesRepository.GetAsync();

        IEnumerable<CourseDto> coursesDto = courses
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TeacherId = c.TeacherId,
            })
            .ToList();

        return coursesDto;
    }

    public async Task<CourseDto> GetByIdAsync(Guid id)
    {
        Course? course = await _coursesRepository
            .GetByIdAsync(id)
            ?? throw new InvalidOperationException();

        CourseDto courseDto = new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
        };

        return courseDto;
    }

    public async Task UpdateByIdAsync(
        Guid id,
        Guid teacherId,
        string title,
        string description)
    {
        bool exists = await _teachersRepository.ExistsByIdAsync(teacherId);

        if (!exists)
            throw new InvalidOperationException();

        int result = await _coursesRepository.UpdateByIdAsync(
            id,
            teacherId,
            title,
            description);

        if (result == 0)
            throw new InvalidOperationException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _coursesRepository.RemoveByIdAsync(id);

        if (result == 0)
            throw new InvalidOperationException();
    }
}