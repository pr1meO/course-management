using Asp.Versioning;
using CourseManagement.Contracts.Courses;
using CourseManagement.Models;
using CourseManagement.Services;
using CourseManagement.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion(1)]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CourseController(
        ICourseService courseService)
    {
        _courseService = courseService;
    }

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
        };

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { courseDto.Id },
            courseDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        PaginationValidator validator = new(pageNumber, pageSize);

        IEnumerable<Course> courses = await _courseService
            .GetAsync(validator.PageNumber, validator.PageSize);

        IEnumerable<CourseDto> coursesDto = courses
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TeacherId = c.TeacherId,
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
