using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusControlMessageProducer : IMessageProducer
{
    private readonly IBusControl busControl;
    public RabbitMqBusControlMessageProducer(IBusControl BusControl)
    {
        busControl = BusControl;

    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        try
        {
            var endpoint = await busControl.GetSendEndpoint(new Uri("rabbitmq://localhost/some-queue"));
            await endpoint.Send(message);
        }
        catch (Exception)
        {

            throw;
        }
        finally
        {
            await busControl.StopAsync();
        }

    }
}
