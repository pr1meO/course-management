using Asp.Versioning;
using CourseManagement.Contracts.Courses;
using CourseManagement.Models;
using CourseManagement.Services;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v2;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[Produces("application/json")]
[ApiVersion(2)]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CourseController(
        ICourseService courseService)
    {
        _courseService = courseService;
    }

    [Idempotent]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        Course course = await _courseService.AddAsync(
            request.Title,
            request.Description,
            request.TeacherId);

        CourseDto courseDto = new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
            CreatedAt = course.CreatedAt,
        };

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { courseDto.Id },
            courseDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        IEnumerable<Course> courses = await _courseService.GetAsync();

        IEnumerable<CourseDto> coursesDto = courses
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TeacherId = c.TeacherId,
                CreatedAt = c.CreatedAt,
            })
            .ToList();

        return Ok(coursesDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        Course course = await _courseService.GetByIdAsync(id);

        CourseDto courseDto = new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
            CreatedAt = course.CreatedAt,
        };

        return Ok(courseDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        await _courseService.UpdateByIdAsync(
            id,
            request.TeacherId,
            request.Title,
            request.Description);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _courseService.RemoveByIdAsync(id);

        return NoContent();
    }
}
