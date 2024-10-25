using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Confluent.Kafka;
using MassTransit;

using Verify.Application.Dtos.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.MessageConsumers;
internal sealed class AccountMessageConsumer : IConsumer<MyMessage>
{
    public AccountMessageConsumer()
    {
            
    }

    public async Task Consume(ConsumeContext<MyMessage> context)
    {
        try
        {
            // Handle the incoming message
            var message = context.Message;
            Console.WriteLine($"Received message: {message.Text}");

            // Add Custom business logic here
            await Task.CompletedTask;
        }
        catch (Exception)
        {

            throw;
        }
    }
}
