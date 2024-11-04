namespace Verify.Application.Abstractions.MessageQueuing;
public interface IMessageConsumer
{
    Task ConsumeAsync<T>(Func<T, Task> handler) where T : class;
}
