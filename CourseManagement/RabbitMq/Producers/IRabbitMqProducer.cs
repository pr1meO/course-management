using System.Text;
using System.Text.Json;
using CourseManagement.Configuration.Options;
using CourseManagement.RabbitMq.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CourseManagement.RabbitMq.Producers;

public interface IRabbitMqProducer
{
    Task<StandardResponseMessage> SendAsync(StandardRequestMessage request);
}

public class RabbitMqProducer : IRabbitMqProducer, IDisposable
{
    private const string RequestsQueue = "api.requests";
    private const string ResponsesQueue = "api.responses";

    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqProducer(RabbitMqOptions options)
    {
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

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "dead_letter_exchange" },
            { "x-dead-letter-routing-key", "dead.letter" },
            { "x-message-ttl", 300000 },
        };

        _channel.QueueDeclare(
            queue: RequestsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args);

        _channel.QueueDeclare(
            queue: ResponsesQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
    }

    public Task<StandardResponseMessage> SendAsync(StandardRequestMessage request)
    {
        var tcs = new TaskCompletionSource<StandardResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var corrId = request.Id.ToString();

        var replyQueue = _channel.QueueDeclare(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null).QueueName;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var incomingCorr = ea.BasicProperties?.CorrelationId;
                if (incomingCorr != corrId)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var response = JsonSerializer.Deserialize<StandardResponseMessage>(json);

                _channel.BasicAck(ea.DeliveryTag, false);

                if (response == null)
                    tcs.TrySetException(new Exception("Empty response"));
                else
                    tcs.TrySetResult(response);
            }
            catch (Exception ex)
            {
                _channel.BasicAck(ea.DeliveryTag, false);
                tcs.TrySetException(ex);
            }
        };

        var consumerTag = _channel.BasicConsume(replyQueue, autoAck: false, consumer: consumer);

        var jsonReq = JsonSerializer.Serialize(request);
        var body = Encoding.UTF8.GetBytes(jsonReq);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.CorrelationId = corrId;
        props.ReplyTo = replyQueue;

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RequestsQueue,
            basicProperties: props,
            body: body);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetException(new TimeoutException("Timeout waiting reply queue"));
            }
            finally
            {
                try
                {
                    _channel.BasicCancel(consumerTag);
                }
                catch
                {
                }

                try
                {
                    _channel.QueueDelete(replyQueue);
                }
                catch
                {
                }
            }
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
