using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusControlMessageProducer : IMessageProducer
{
    private readonly IBusControl _busControl;
    public RabbitMqBusControlMessageProducer(IBusControl busControl)
    {
        _busControl = busControl;

    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        try
        {
            var endpoint = await _busControl.GetSendEndpoint(new Uri("rabbitmq://localhost/some-queue"));
            await endpoint.Send(message);
        }
        finally
        {
            await _busControl.StopAsync();
        }

    }
}
