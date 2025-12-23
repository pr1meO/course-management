using System.Text.Json.Serialization;

namespace CourseManagement.RabbitMq.Messages;

public class StandardResponseMessage
{
    [JsonPropertyName("correlation_id")]
    public Guid CorrelationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
