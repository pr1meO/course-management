using System.Text.Json.Serialization;

namespace CourseManagement.RabbitMq.Messages;

public class StandardRequestMessage
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("version")]
    public string Version { get; set; } = "v1";

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("auth")]
    public string? Auth { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}