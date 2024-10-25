using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using Confluent.Kafka;

using Newtonsoft.Json;
using Verify.Application.Abstractions.MessageQueuing;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Verify.Infrastructure.Implementations.MessageQueuing.Transports.Kafka;
internal sealed class KafkaMessageProducer : IMessageProducer
{
    private readonly IProducer<string, byte[]> producer;
    public KafkaMessageProducer(ProducerConfig ProducerConfig)
    {
        // Ensure producerConfig is valid
        var producerBuilder = new ProducerBuilder<string, byte[]>(ProducerConfig);
        producer = producerBuilder.Build(); // Correctly build the Kafka producer
    }

    public async Task ProduceAsync<T>(T message) where T : class
    {
        try
        {
            var topic = typeof(T).Name;
            var serializedMessage = Serialize(message);

            var deliveryReport = await producer.ProduceAsync(topic, new Message<string, byte[]>
            {
                Key = Guid.NewGuid().ToString(),
                Value = serializedMessage
            });

            Console.WriteLine($"Message delivered to {deliveryReport.TopicPartitionOffset}");
        }
        catch (ProduceException<string, byte[]>)
        {
            // Handle Kafka-specific exceptions

            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private byte[] Serialize<T>(T message)
    {
        // Use a serialization strategy; here JSON is used as an example
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }
}
