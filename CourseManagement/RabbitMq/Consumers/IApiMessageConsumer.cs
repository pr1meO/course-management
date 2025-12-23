using CourseManagement.Configuration.Options;
using CourseManagement.RabbitMq.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CourseManagement.RabbitMq.Consumers;

public interface IApiMessageConsumer
{
    void StartConsuming();

    void StopConsuming();
}

public class ApiMessageConsumer : IApiMessageConsumer, IDisposable
{
    private const string DeadLetterExchange = "dead_letter_exchange";
    private const string DeadLetterQueue = "dead_letter_queue";
    private const string DeadLetterRoutingKey = "dead.letter";

    private const string RequestQueueName = "api.requests";

    private readonly IServiceProvider _sp;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IModel _dlqChannel;
    private readonly ILogger<ApiMessageConsumer> _logger;

    private EventingBasicConsumer? _consumer;
    private bool _isConsuming;

    public ApiMessageConsumer(
        IServiceProvider sp,
        ILogger<ApiMessageConsumer> logger,
        RabbitMqOptions options)
    {
        _sp = sp;
        _logger = logger;

        ConnectionFactory factory = new()
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _dlqChannel = _connection.CreateModel();

        // DLQ
        _dlqChannel.ExchangeDeclare(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        _dlqChannel.QueueDeclare(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _dlqChannel.QueueBind(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterRoutingKey);

        // Основная очередь запросов
        Dictionary<string, object> args = new()
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", DeadLetterRoutingKey },
            { "x-message-ttl", 300000 }, // 5 минут
        };

        _channel.QueueDeclare(
            queue: RequestQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args);
    }

    public void StartConsuming()
    {
        if (_isConsuming)
            return;

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += async (_, ea) =>
        {
            using IServiceScope scope = _sp.CreateScope();
            IMessageProcessingService processor = scope.ServiceProvider
                .GetRequiredService<IMessageProcessingService>();

            await processor.ProcessMessageAsync(ea, _sp, _channel);
        };

        _channel.BasicConsume(
            queue: RequestQueueName,
            autoAck: false,
            consumer: _consumer);

        _isConsuming = true;

        _logger.LogInformation("Consumer started, listening: {Queue}", RequestQueueName);
    }

    public void StopConsuming()
    {
        if (!_isConsuming || _consumer == null)
            return;

        _channel.BasicCancel(_consumer.ConsumerTags.First());
        _isConsuming = false;

        _logger.LogInformation("Consumer stopped");
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Close();
        _dlqChannel?.Close();
        _connection?.Close();
    }
}