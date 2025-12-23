using System.Text;
using System.Text.Json;
using CourseManagement.Contracts.Courses;
using CourseManagement.Contracts.Teachers;
using CourseManagement.Models;
using CourseManagement.RabbitMq.Messages;
using CourseManagement.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CourseManagement.RabbitMq.Services;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(
        BasicDeliverEventArgs ea,
        IServiceProvider sp,
        IModel channel);
}

public class MessageProcessingService : IMessageProcessingService
{
    private const int MaxRetryCount = 3;
    private const int BaseDelayMs = 1000;

    private readonly ILogger<MessageProcessingService> _logger;

    public MessageProcessingService(
        ILogger<MessageProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        BasicDeliverEventArgs ea,
        IServiceProvider sp,
        IModel channel)
    {
        string messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using IServiceScope scope = sp.CreateScope();

            ITeacherService teacherService = scope.ServiceProvider
                .GetRequiredService<ITeacherService>();
            ICourseService courseService = scope.ServiceProvider
                .GetRequiredService<ICourseService>();
            IIdempotencyService idempotencyService = scope.ServiceProvider
                .GetRequiredService<IIdempotencyService>();

            StandardRequestMessage? request = JsonSerializer.Deserialize<StandardRequestMessage>(messageJson);

            if (request is null)
            {
                _logger.LogError("Bad request (cannot deserialize): {Json}", messageJson);

                channel.BasicNack(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false);

                return;
            }

            if (idempotencyService != null && await idempotencyService.IsProcessedAsync(request.Id))
            {
                StandardResponseMessage cached = new()
                {
                    CorrelationId = request.Id,
                    Status = "ok",
                    Data = new { message = "Already processed", is_cached = true },
                    Error = null,
                };

                await SendResponseAsync(cached, ea, channel);

                channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);

                return;
            }

            if (!IsAuthorized(request.Auth))
            {
                StandardResponseMessage unauthorized = new()
                {
                    CorrelationId = request.Id,
                    Status = "error",
                    Data = null,
                    Error = "unauthorized",
                };

                await SendResponseAsync(unauthorized, ea, channel);

                channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);

                return;
            }

            (bool ok, object? data, string? error) = await HandleAction(
                request,
                teacherService,
                courseService);

            if (idempotencyService is not null && ok)
            {
                await idempotencyService.MarkAsProcessedAsync(request.Id);
            }

            StandardResponseMessage response = new()
            {
                CorrelationId = request.Id,
                Status = ok ? "ok" : "error",
                Data = data,
                Error = ok ? null : error,
            };

            await SendResponseAsync(response, ea, channel);

            channel.BasicAck(
                deliveryTag: ea.DeliveryTag,
                multiple: false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON: {Json}", messageJson);
            SendToDlq(channel, messageJson, ex.Message);

            channel.BasicNack(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");

            var retryCount = GetRetryCountFromHeader(ea);

            if (retryCount >= MaxRetryCount)
            {
                var corrIdStr = ea.BasicProperties?.CorrelationId;
                Guid corrId = Guid.TryParse(corrIdStr, out var g) ? g : Guid.Empty;

                var response = new StandardResponseMessage
                {
                    CorrelationId = corrId,
                    Status = "error",
                    Data = null,
                    Error = "failed after retries; moved to DLQ",
                };

                await SendResponseAsync(response, ea, channel);

                SendToDlq(channel, messageJson, ex.Message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            await Task.Delay(CalculateDelay(retryCount));

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.CorrelationId = ea.BasicProperties?.CorrelationId;
            props.ReplyTo = ea.BasicProperties?.ReplyTo;

            props.Headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object>();
            props.Headers["x-retry-count"] = (retryCount + 1).ToString();

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: "api.requests",
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(messageJson));

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    private static bool IsAuthorized(string? auth)
    {
        const string ApiKey = "KM]>Q^![e|9lUT6&G)x?!x%cE^}[z)UpuSmV)Z_T!Pf4;@n!@:I1h,/GSj#Vl}";

        return !string.IsNullOrWhiteSpace(auth) && auth == ApiKey;
    }

    private static async Task<(bool ok, object? data, string? error)> HandleAction(
        StandardRequestMessage request,
        ITeacherService teacherService,
        ICourseService courseService)
    {
        JsonElement dataEl = GetDataElement(request.Data);

        switch (request.Action)
        {
            case "create_teacher":
                {
                    //throw new Exception();

                    Teacher teacher = await teacherService.AddAsync(
                        GetString(dataEl, "login"),
                        GetString(dataEl, "password_hash"),
                        GetString(dataEl, "first_name"),
                        GetString(dataEl, "last_name"),
                        GetStringOrNull(dataEl, "middle_name") ?? string.Empty);

                    return (true, new { teacher_id = teacher.Id }, null);
                }

            case "get_teacher":
                {
                    Teacher teacher = await teacherService.GetByIdAsync(GetGuid(dataEl, "teacher_id"));

                    TeacherDto teacherDto = new()
                    {
                        Id = teacher.Id,
                        Login = teacher.Login,
                        FirstName = teacher.FirstName,
                        LastName = teacher.LastName,
                        MiddleName = teacher.MiddleName,
                    };

                    return (true, teacherDto, null);
                }

            case "get_teachers":
                {
                    IEnumerable<Teacher> teachers = await teacherService.GetAsync(
                        GetInt(dataEl, "page_number", 1),
                        GetInt(dataEl, "page_size", 10));

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

                    return (true, teachersDto, null);
                }

            case "update_teacher":
                {
                    await teacherService.UpdateByIdAsync(
                        GetGuid(dataEl, "teacher_id"),
                        GetString(dataEl, "login"),
                        GetString(dataEl, "first_name"),
                        GetString(dataEl, "last_name"),
                        GetStringOrNull(dataEl, "middle_name") ?? string.Empty);

                    return (true, new { success = true }, null);
                }

            case "delete_teacher":
                {
                    await teacherService.RemoveByIdAsync(GetGuid(dataEl, "teacher_id"));

                    return (true, new { success = true }, null);
                }

            case "create_course":
                {
                    Course course = await courseService.AddAsync(
                        GetString(dataEl, "title"),
                        GetString(dataEl, "description"),
                        GetGuid(dataEl, "teacher_id"));

                    return (true, new { course_id = course.Id }, null);
                }

            case "get_course":
                {
                    Course course = await courseService.GetByIdAsync(
                        GetGuid(dataEl, "course_id"));

                    CourseDto courseDto = new()
                    {
                        Id = course.Id,
                        Title = course.Title,
                        Description = course.Description,
                        TeacherId = course.TeacherId,
                        CreatedAt = course.CreatedAt,
                    };

                    return (true, courseDto, null);
                }

            case "get_courses":
                {
                    IEnumerable<Course> courses = await courseService.GetAsync(
                        GetInt(dataEl, "page_number", 1),
                        GetInt(dataEl, "page_size", 10));

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

                    return (true, coursesDto, null);
                }

            case "update_course":
                {
                    await courseService.UpdateByIdAsync(
                        GetGuid(dataEl, "course_id"),
                        GetGuid(dataEl, "teacher_id"),
                        GetString(dataEl, "title"),
                        GetString(dataEl, "description"));

                    return (true, new { success = true }, null);
                }

            case "delete_course":
                {
                    await courseService.RemoveByIdAsync(
                        GetGuid(dataEl, "course_id"));

                    return (true, new { success = true }, null);
                }

            default:
                return (false, null, $"Unknown action: {request.Action}");
        }
    }

    private static async Task SendResponseAsync(
        StandardResponseMessage response,
        BasicDeliverEventArgs ea,
        IModel channel)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        IBasicProperties props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.CorrelationId = ea.BasicProperties.CorrelationId ??
            response.CorrelationId.ToString();

        var replyTo = ea.BasicProperties?.ReplyTo;
        var targetQueue = string.IsNullOrWhiteSpace(replyTo) ? "api.responses" : replyTo;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: targetQueue,
            basicProperties: props,
            body: body);

        await Task.CompletedTask;
    }

    private static int GetRetryCountFromHeader(BasicDeliverEventArgs ea)
    {
        var headers = ea.BasicProperties?.Headers;
        if (headers == null) return 0;

        if (!headers.TryGetValue("x-retry-count", out var raw)) return 0;

        if (raw is byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            return int.TryParse(s, out var n) ? n : 0;
        }

        if (raw is int i) return i;

        return 0;
    }

    private static int CalculateDelay(int retryCount) =>
        BaseDelayMs * (int)Math.Pow(2, retryCount);

    private static void SendToDlq(IModel channel, string messageJson, string error)
    {
        const string DeadLetterExchange = "dead_letter_exchange";
        const string DeadLetterRoutingKey = "dead.letter";

        IBasicProperties props = channel.CreateBasicProperties();

        props.Headers = new Dictionary<string, object>
        {
            { "original_error", error },
            { "timestamp", DateTime.UtcNow.ToString("O") },
        };

        channel.BasicPublish(
            exchange: DeadLetterExchange,
            routingKey: DeadLetterRoutingKey,
            props,
            Encoding.UTF8.GetBytes(messageJson));
    }

    private static JsonElement GetDataElement(object? data)
    {
        if (data == null)
            return default;

        string json = JsonSerializer.Serialize(data);

        return JsonDocument.Parse(json).RootElement;
    }

    private static string GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out JsonElement p))
            throw new Exception($"Missing field: {name}");

        return p.GetString() ?? string.Empty;
    }

    private static string? GetStringOrNull(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out JsonElement p))
            return null;

        return p.ValueKind == JsonValueKind.Null ? null : p.GetString();
    }

    private static Guid GetGuid(JsonElement el, string name)
    {
        string s = GetString(el, name);

        return Guid.Parse(s);
    }

    private static int GetInt(JsonElement el, string name, int def)
    {
        if (!el.TryGetProperty(name, out JsonElement p))
            return def;

        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int v))
            return v;

        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out int s))
            return s;

        return def;
    }
}