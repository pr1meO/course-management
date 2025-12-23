using Asp.Versioning;
using CourseManagement.Configuration.Options;
using CourseManagement.Contracts.Courses;
using CourseManagement.Contracts.Teachers;
using CourseManagement.RabbitMq.Messages;
using CourseManagement.RabbitMq.Producers;
using CourseManagement.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CourseManagement.Controllers.v2;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/rabbit")]
[Produces("application/json")]
[ApiVersion(2)]
public class RabbitController : ControllerBase
{
    private readonly IRabbitMqProducer _producer;
    private readonly IPasswordHasher _hasher;
    private readonly TokenOptions _options;

    public RabbitController(
        IRabbitMqProducer producer,
        IPasswordHasher hasher,
        IOptions<TokenOptions> options)
    {
        _producer = producer;
        _hasher = hasher;
        _options = options.Value;
    }

    [HttpPost("teachers")]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherRequest request)
    {
        StandardRequestMessage message = new()
        {
            // Guid.Parse("11111111-1111-1111-1111-111111111111")
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "create_teacher",
            Auth = _options.Key,
            Data = new
            {
                login = request.Login,
                password_hash = _hasher.Generate(request.Password),
                first_name = request.FirstName,
                last_name = request.LastName,
                middle_name = request.MiddleName,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok"
            ? Ok(response.Data)
            : BadRequest(response.Error);
    }

    [HttpGet("teachers/{id:guid}")]
    public async Task<IActionResult> GetTeacherById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_teacher",
            Auth = _options.Key,
            Data = new
            {
                teacher_id = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok"
            ? Ok(response.Data)
            : BadRequest(response.Error);
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_teachers",
            Auth = _options.Key,
            Data = new
            {
                page_number = pageNumber,
                page_size = pageSize,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpPut("teachers/{id:guid}")]
    public async Task<IActionResult> UpdateTeacherById(
        Guid id,
        [FromBody] UpdateTeacherRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "update_teacher",
            Auth = _options.Key,
            Data = new
            {
                teacher_id = id.ToString(),
                login = request.Login,
                first_name = request.FirstName,
                last_name = request.LastName,
                middle_name = request.MiddleName,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpDelete("teachers/{id:guid}")]
    public async Task<IActionResult> DeleteTeacherById([FromRoute] Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "delete_teacher",
            Auth = _options.Key,
            Data = new
            {
                teacher_id = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "create_course",
            Auth = _options.Key,
            Data = new
            {
                title = request.Title,
                description = request.Description,
                teacher_id = request.TeacherId.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpGet("courses/{id:guid}")]
    public async Task<IActionResult> GetCoursetById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_course",
            Auth = _options.Key,
            Data = new
            {
                course_id = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_courses",
            Auth = _options.Key,
            Data = new
            {
                page_number = pageNumber,
                page_size = pageSize,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCoursetById(
        Guid id,
        [FromBody] UpdateCourseRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "update_course",
            Auth = _options.Key,
            Data = new
            {
                course_id = id.ToString(),
                teacher_id = request.TeacherId.ToString(),
                title = request.Title,
                description = request.Description,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCoursetById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "delete_course",
            Auth = _options.Key,
            Data = new
            {
                course_id = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "ok" ?
            Ok(response.Data) :
            BadRequest(response.Error);
    }
}
