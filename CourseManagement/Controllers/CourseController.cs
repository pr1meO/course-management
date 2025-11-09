using CourseManagement.Contracts.Courses;
using CourseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/courses")]
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

        CourseDto courseDto = await _courseService.AddAsync(
            request.Title,
            request.Description,
            request.TeacherId);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { courseDto.Id },
            courseDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        IEnumerable<CourseDto> courseDtos = await _courseService.GetAsync();

        return Ok(courseDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        CourseDto courseDto = await _courseService.GetByIdAsync(id);

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

        return Ok(new
        {
            Message = "Resource updated successfully.",
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _courseService.RemoveByIdAsync(id);

        return Ok(new
        {
            Message = "Resource deleted successfully.",
        });
    }
}
