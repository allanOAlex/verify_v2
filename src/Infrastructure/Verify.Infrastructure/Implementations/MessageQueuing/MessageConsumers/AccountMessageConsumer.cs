using MassTransit;

using Verify.Application.Dtos.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.MessageConsumers;
internal sealed class AccountMessageConsumer : IConsumer<MyMessage>
{
    public async Task Consume(ConsumeContext<MyMessage> context)
    {
        // Handle the incoming message
        var message = context.Message;
        Console.WriteLine($"Received message: {message.Text}");

        // Add Custom business logic here
        await Task.CompletedTask;
    }
}
