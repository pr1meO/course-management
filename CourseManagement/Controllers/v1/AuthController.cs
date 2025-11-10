using Asp.Versioning;
using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion(1)]
public class AuthController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public AuthController(
        ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAsync([FromBody] CreateTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.MiddleName) ||
            string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        Teacher teacher = await _teacherService.AddAsync(
            request.Login,
            request.Password,
            request.LastName,
            request.FirstName,
            request.MiddleName);

        TeacherDto teacherDto = new()
        {
            Id = teacher.Id,
            Login = teacher.Login,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
        };

        // TODO: JWT Token Access
        return CreatedAtAction(
            nameof(TeacherController.GetByIdAsync),
            nameof(Teacher),
            new { teacherDto.Id },
            teacherDto);
    }
}