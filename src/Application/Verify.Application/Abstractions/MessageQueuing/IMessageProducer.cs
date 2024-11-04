namespace Verify.Application.Abstractions.MessageQueuing;
public interface IMessageProducer
{
    Task ProduceAsync<T>(T message) where T : class;
}
