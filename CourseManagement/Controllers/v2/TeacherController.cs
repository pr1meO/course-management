using Asp.Versioning;
using CourseManagement.Contracts;
using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v2;

[Authorize(AuthenticationSchemes = "Access")]
[ApiController]
[Route("api/v{version:apiVersion}/teachers")]
[ApiVersion(2)]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeacherController(
        ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TeacherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAsync()
    {
        IEnumerable<Teacher> teachers = await _teacherService.GetAsync();

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

        return Ok(teachersDto);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeacherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        Teacher teacher = await _teacherService.GetByIdAsync(id);

        TeacherDto teacherDto = new()
        {
            Id = teacher.Id,
            Login = teacher.Login,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
        };

        return Ok(teacherDto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.MiddleName) ||
            string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid request data.",
            });
        }

        await _teacherService.UpdateByIdAsync(
            id,
            request.Login,
            request.FirstName,
            request.LastName,
            request.MiddleName);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _teacherService.RemoveByIdAsync(id);

        return NoContent();
    }
}
