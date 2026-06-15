using AutonomusCRM.Infrastructure.DataHub;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace AutonomusCRM.Tests.DataHub;

/// <summary>In-process RabbitMQ channel for consumer tests when no broker is available.</summary>
internal sealed class InMemoryRabbitConsumeChannel : IRabbitConsumeChannel
{
    private readonly ConcurrentQueue<BasicGetResult> _queue = new();
    private readonly string _dlqName;

    public InMemoryRabbitConsumeChannel(string deadLetterQueueName) => _dlqName = deadLetterQueueName;

    public IReadOnlyList<byte[]> DeadLetters => _deadLetterBodies;
    private readonly List<byte[]> _deadLetterBodies = new();

    public void Enqueue(BasicGetResult message) => _queue.Enqueue(message);

    public BasicGetResult? BasicGet(string queue, bool autoAck)
        => _queue.TryDequeue(out var result) ? result : null;

    public void BasicAck(ulong deliveryTag, bool multiple)
    {
    }

    public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
    {
    }

    public IBasicProperties CreateBasicProperties() => new InMemoryBasicProperties();

    public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
    {
        if (string.Equals(routingKey, _dlqName, StringComparison.Ordinal))
            _deadLetterBodies.Add(body.ToArray());
    }

    internal static BasicGetResult CreateGetResult(
        string queue,
        byte[] body,
        IBasicProperties? properties = null,
        ulong deliveryTag = 1)
        => new(deliveryTag, false, "", queue, 0, properties ?? new InMemoryBasicProperties(), body);

    private sealed class InMemoryBasicProperties : IBasicProperties
    {
        public ushort ProtocolClassId { get; set; }
        public string ProtocolClassName { get; set; } = "basic";
        public string? AppId { get; set; }
        public string? ClusterId { get; set; }
        public string? ContentEncoding { get; set; }
        public string? ContentType { get; set; }
        public string? CorrelationId { get; set; }
        public byte DeliveryMode { get; set; }
        public string? Expiration { get; set; }
        public IDictionary<string, object?>? Headers { get; set; } = new Dictionary<string, object?>();
        public bool IsHeadersPresent() => Headers is { Count: > 0 };
        public bool IsAppIdPresent() => AppId != null;
        public bool IsClusterIdPresent() => ClusterId != null;
        public bool IsContentEncodingPresent() => ContentEncoding != null;
        public bool IsContentTypePresent() => ContentType != null;
        public bool IsCorrelationIdPresent() => CorrelationId != null;
        public bool IsDeliveryModePresent() => true;
        public bool IsExpirationPresent() => Expiration != null;
        public bool IsMessageIdPresent() => MessageId != null;
        public bool IsPersistentPresent() => true;
        public bool IsPriorityPresent() => true;
        public bool IsReplyToPresent() => ReplyTo != null;
        public bool IsTimestampPresent() => Timestamp.UnixTime != 0;
        public bool IsTypePresent() => Type != null;
        public bool IsUserIdPresent() => UserId != null;
        public string? MessageId { get; set; }
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string? ReplyTo { get; set; }
        public PublicationAddress? ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; }
        public string? Type { get; set; }
        public string? UserId { get; set; }
        public void ClearAppId() => AppId = null;
        public void ClearClusterId() => ClusterId = null;
        public void ClearContentEncoding() => ContentEncoding = null;
        public void ClearContentType() => ContentType = null;
        public void ClearCorrelationId() => CorrelationId = null;
        public void ClearDeliveryMode() => DeliveryMode = 0;
        public void ClearExpiration() => Expiration = null;
        public void ClearHeaders() => Headers = new Dictionary<string, object?>();
        public void ClearMessageId() => MessageId = null;
        public void ClearPriority() => Priority = 0;
        public void ClearReplyTo() => ReplyTo = null;
        public void ClearTimestamp() => Timestamp = default;
        public void ClearType() => Type = null;
        public void ClearUserId() => UserId = null;
    }
}
