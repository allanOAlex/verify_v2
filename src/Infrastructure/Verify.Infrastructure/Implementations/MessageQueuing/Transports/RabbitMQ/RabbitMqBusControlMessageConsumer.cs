using MassTransit;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
internal sealed class RabbitMqBusControlMessageConsumer : IMessageConsumer
{
    private readonly IBusControl _busControl;
    public RabbitMqBusControlMessageConsumer(IBusControl busControl)
    {
        _busControl = busControl;

    }

    public async Task ConsumeAsync<T>(Func<T, Task> handler) where T : class
    {
        // Start the bus
        await _busControl.StartAsync();

        try
        {
            var queueName = typeof(T).Name;
            _busControl.ConnectReceiveEndpoint(queueName, cfg =>
            {
                cfg.Handler<T>(context => handler(context.Message));
            });
        }
        finally
        {
            await _busControl.StopAsync();
        }
    }
}
