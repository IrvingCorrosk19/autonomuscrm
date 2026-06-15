using RabbitMQ.Client;

namespace AutonomusCRM.Infrastructure.DataHub;

/// <summary>Minimal RabbitMQ surface used by <see cref="DataHubRabbitImportConsumer"/> (testable without a broker).</summary>
internal interface IRabbitConsumeChannel
{
    BasicGetResult? BasicGet(string queue, bool autoAck);
    void BasicAck(ulong deliveryTag, bool multiple);
    void BasicNack(ulong deliveryTag, bool multiple, bool requeue);
    IBasicProperties CreateBasicProperties();
    void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body);
}

internal sealed class RabbitModelConsumeAdapter : IRabbitConsumeChannel
{
    private readonly IModel _model;

    public RabbitModelConsumeAdapter(IModel model) => _model = model;

    public BasicGetResult? BasicGet(string queue, bool autoAck) => _model.BasicGet(queue, autoAck);

    public void BasicAck(ulong deliveryTag, bool multiple) => _model.BasicAck(deliveryTag, multiple);

    public void BasicNack(ulong deliveryTag, bool multiple, bool requeue) => _model.BasicNack(deliveryTag, multiple, requeue);

    public IBasicProperties CreateBasicProperties() => _model.CreateBasicProperties();

    public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        => _model.BasicPublish(exchange, routingKey, basicProperties, body);
}
