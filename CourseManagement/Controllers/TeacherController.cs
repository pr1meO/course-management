using CourseManagement.Contracts.Teachers;
using CourseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/teachers")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeacherController(
        ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        IEnumerable<TeacherDto> teacherDtos = await _teacherService.GetAsync();

        return Ok(teacherDtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        TeacherDto teacherDto = await _teacherService.GetByIdAsync(id);

        return Ok(teacherDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.MiddleName) ||
            string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        await _teacherService.UpdateByIdAsync(
            id,
            request.Login,
            request.FirstName,
            request.LastName,
            request.MiddleName);

        return Ok(new
        {
            Message = "Resource updated successfully.",
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _teacherService.RemoveByIdAsync(id);

        return Ok(new
        {
            Message = "Resource deleted successfully.",
        });
    }
}
