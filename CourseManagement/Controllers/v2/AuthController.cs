using Asp.Versioning;
using CourseManagement.Contracts;
using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.Services.Auth;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Controllers.v2;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[ApiVersion(2)]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [Idempotent]
    [HttpPost("register")]
    [ProducesResponseType(typeof(TeacherDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RegisterAsync([FromBody] CreateTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.MiddleName) ||
            string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid request data.",
            });
        }

        Teacher teacher = await _identityService.RegisterAsync(
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

        return CreatedAtAction(
            nameof(TeacherController.GetByIdAsync),
            nameof(Teacher),
            new { teacherDto.Id },
            teacherDto);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid request data.",
            });
        }

        JwtTokenResponse response = new()
        {
            Access = await _identityService.LoginAsync(
                request.Login,
                request.Password),
        };

        return Ok(response);
    }
}