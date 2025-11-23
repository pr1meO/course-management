using System.Dynamic;
using Asp.Versioning;
using CourseManagement.Contracts;
using CourseManagement.Contracts.Courses;
using CourseManagement.Models;
using CourseManagement.Services;
using CourseManagement.Validators;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v2;

[Authorize(AuthenticationSchemes = "Access")]
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[Produces("application/json")]
[ApiVersion(2)]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IDataShaper<CourseDto> _dataShaper;

    public CourseController(
        ICourseService courseService,
        IDataShaper<CourseDto> dataShaper)
    {
        _courseService = courseService;
        _dataShaper = dataShaper;
    }

    [Idempotent]
    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
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
    [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ExpandoObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string? fields,
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
                CreatedAt = c.CreatedAt,
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(fields))
            return Ok(coursesDto);

        IEnumerable<ExpandoObject> shapedData = _dataShaper.ShapeData(coursesDto, fields);

        return Ok(shapedData);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExpandoObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        [FromQuery] string? fields)
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

        if (string.IsNullOrWhiteSpace(fields))
            return Ok(courseDto);

        ExpandoObject shapedObject = _dataShaper.ShapeData(courseDto, fields);

        return Ok(shapedObject);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _courseService.RemoveByIdAsync(id);

        return NoContent();
    }
}
