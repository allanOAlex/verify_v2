using System.Text;
using Confluent.Kafka;

using Newtonsoft.Json;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.Kafka;
internal sealed class KafkaMessageConsumer : IMessageConsumer
{
    private readonly IConsumer<string, byte[]> _consumer;
    public KafkaMessageConsumer(ConsumerConfig consumerConfig)
    {
        // Create the Kafka consumer using ConsumerBuilder
        var consumerBuilder = new ConsumerBuilder<string, byte[]>(consumerConfig);
        _consumer = consumerBuilder.Build();
    }
    //ToDO:
    public async Task ConsumeAsync<T>(Func<T, Task> handler) where T : class
    {
        var topic = typeof(T).Name;
        _consumer.Subscribe(topic);

        try
        {
            while (true)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult != null)
                {
                    var deserializedMessage = Deserialize<T>(consumeResult.Message.Value);
                    if (deserializedMessage != null)
                    {
                        await handler(deserializedMessage);

                    }
                    else
                    {
                        ArgumentException.ThrowIfNullOrEmpty(nameof(handler));
                    }
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private T? Deserialize<T>(byte[] data)
    {
        // Deserialization using JSON (can be changed to a preferred format)

        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));

    }

    

}
