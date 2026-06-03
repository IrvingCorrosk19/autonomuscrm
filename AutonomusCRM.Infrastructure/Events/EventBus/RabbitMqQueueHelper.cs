using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AutonomusCRM.Infrastructure.Events.EventBus;

/// <summary>
/// Declares durable queues with DLX; recreates when RabbitMQ reports PRECONDITION_FAILED (arg drift).
/// </summary>
internal static class RabbitMqQueueHelper
{
    private const ushort PreconditionFailed = 406;

    public static void DeclareMainQueue(
        IModel channel,
        string queueName,
        string exchange,
        string routingKey,
        string dlx,
        ILogger logger)
    {
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlx,
            ["x-dead-letter-routing-key"] = $"{routingKey}.failed"
        };

        try
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == PreconditionFailed)
        {
            logger.LogWarning(
                "Queue {Queue} incompatible ({Reason}); deleting and recreating with DLX",
                queueName,
                ex.ShutdownReason?.ReplyText);

            try
            {
                channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
            }
            catch (Exception delEx)
            {
                logger.LogWarning(delEx, "QueueDelete failed for {Queue}", queueName);
            }

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        }

        channel.QueueBind(queueName, exchange, routingKey);
    }

    public static void DeclareDlq(IModel channel, string dlqName, string dlx, string failedRoutingKey)
    {
        channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, dlx, failedRoutingKey);
    }
}
