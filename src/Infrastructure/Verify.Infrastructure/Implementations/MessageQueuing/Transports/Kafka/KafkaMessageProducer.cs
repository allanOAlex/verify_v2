using System.Text;
using Confluent.Kafka;

using Newtonsoft.Json;
using Verify.Application.Abstractions.MessageQueuing;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.Kafka;
internal sealed class KafkaMessageProducer : IMessageProducer
{
    private readonly IProducer<string, byte[]> _producer;
    public KafkaMessageProducer(ProducerConfig producerConfig)
    {
        // Ensure producerConfig is valid
        var producerBuilder = new ProducerBuilder<string, byte[]>(producerConfig);
        _producer = producerBuilder.Build(); // Correctly build the Kafka producer
    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        var topic = typeof(T).Name;
        var serializedMessage = Serialize(message);

        var deliveryReport = await _producer.ProduceAsync(topic, new Message<string, byte[]>
        {
            Key = Guid.NewGuid().ToString(),
            Value = serializedMessage
        });

        Console.WriteLine($"Message delivered to {deliveryReport.TopicPartitionOffset}");
    }

    private byte[] Serialize<T>(T message)
    {
        // Use a serialization strategy; here JSON is used as an example
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }
}
