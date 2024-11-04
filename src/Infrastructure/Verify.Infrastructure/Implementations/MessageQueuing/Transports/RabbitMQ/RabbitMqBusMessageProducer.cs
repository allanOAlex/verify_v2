using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusMessageProducer : IMessageProducer
{
    private readonly IBus _bus;
    public RabbitMqBusMessageProducer(IBus bus)
    {
        _bus = bus;
    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        await _bus.Publish(message);

        // Sending to a specific queue
        var endpoint = await _bus.GetSendEndpoint(new Uri("rabbitmq://localhost/some-queue"));
        await endpoint.Send(message);
    }
}
