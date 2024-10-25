using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusControlMessageConsumer : IMessageConsumer
{
    private readonly IBusControl busControl;
    public RabbitMqBusControlMessageConsumer(IBusControl BusControl)
    {
        busControl = BusControl;

    }

    public async Task ConsumeAsync<T>(Func<T, Task> handler) where T : class
    {
        // Start the bus
        await busControl.StartAsync();

        try
        {
            var queueName = typeof(T).Name;
            busControl.ConnectReceiveEndpoint(queueName, cfg =>
            {
                cfg.Handler<T>(context => handler(context.Message));
            });
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
