using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusMessageProducer : IMessageProducer
{
    private readonly IBus bus;
    public RabbitMqBusMessageProducer(IBus Bus)
    {
        bus = Bus;
    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        try
        {
            await bus.Publish(message);

            // Sending to a specific queue
            var endpoint = await bus.GetSendEndpoint(new Uri("rabbitmq://localhost/some-queue"));
            await endpoint.Send(message);

        }
        catch (Exception)
        {

            throw;
        }
    }
}
